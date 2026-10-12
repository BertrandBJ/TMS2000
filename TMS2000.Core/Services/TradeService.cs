using TMS2000.Core.Exceptions;
using TMS2000.Core.Helper;
using TMS2000.Core.Models;
using TMS2000.Core.Repositories;
using TMS2000.Shared.DTO.Trades;
using TMS2000.Shared;

namespace TMS2000.Core.Services;

public class TradeService
{
    private readonly UserRepository _userRepository;
    private readonly ResourceRepository _resourceRepository;

    public TradeService(UserRepository userRepository, ResourceRepository resourceRepository)
    {
        _userRepository = userRepository;
        _resourceRepository = resourceRepository;
    }

    public async Task<TradeResponse> ExecuteAsync(string userId, TradeRequest request)
    {
        var type = request.Type!.Value; // non null : garanti par [Required] dans le DTO

        for (var attempt = 1; attempt <= GameSettings.TradeMaxAttempts; attempt++)
        {
            var user = await _userRepository.GetByIdAsync(userId)
                       ?? throw new NotFoundException("Joueur introuvable.");

            
            var existing = user.Transactions.FirstOrDefault(t => t.TransactionId == request.TransactionId);
            if (existing != null)
                return ToResponse(existing, user.Balance);

            var resource = await _resourceRepository.GetByIdAsync(request.ResourceId)
                           ?? throw new NotFoundException($"Ressource '{request.ResourceId}' introuvable.");

            var now = DateTime.UtcNow;
            var transaction = type == TransactionType.Buy
                ? Buy(user, resource, request, now)
                : Sell(user, resource, request, now);

            
            
            user.Transactions.Add(transaction);
            if (user.Transactions.Count > GameSettings.MaxTransactionsPerUser)
                user.Transactions.RemoveRange(0, user.Transactions.Count - GameSettings.MaxTransactionsPerUser);

            var expectedVersion = user.Version;
            user.Version++;

            if (await _userRepository.ReplaceIfVersionMatchesAsync(user, expectedVersion))
                return ToResponse(transaction, user.Balance);

         
        }

        throw new ConflictException("Le compte a été modifié par une autre opération en même temps. Réessayez.");
    }

    private static Transaction Buy(User user, Resource resource, TradeRequest request, DateTime now)
    {
        var price = resource.CurrentPrice;
        EnsureAllowedByRules(resource, now, price, TransactionType.Buy, request.Quantity);

        var taxRate = MarketRules.TaxRate(resource.Rules, now, price);
        var cost = Math.Round(price * request.Quantity * (1 + taxRate), 2);

        if (cost > user.Balance)
            throw new BusinessRuleException($"Solde insuffisant : {cost} nécessaires, {user.Balance} disponibles.");

        user.Balance -= cost;

        var holding = user.Portfolio.FirstOrDefault(h => h.ResourceId == resource.Id);
        if (holding == null)
        {
            holding = new Holding { ResourceId = resource.Id, ResourceName = resource.Name };
            user.Portfolio.Add(holding);
        }

      
        holding.Lots.Add(new Lot
        {
            Quantity = request.Quantity,
            BuyPrice = Math.Round(cost / request.Quantity, 4),
            BoughtAt = now
        });

        return NewTransaction(request, resource, TransactionType.Buy, price, now);
    }

    private static Transaction Sell(User user, Resource resource, TradeRequest request, DateTime now)
    {
        var price = resource.CurrentPrice;
        EnsureAllowedByRules(resource, now, price, TransactionType.Sell, request.Quantity);

        var holding = user.Portfolio.FirstOrDefault(h => h.ResourceId == resource.Id);
        var owned = holding?.Lots.Sum(lot => lot.Quantity) ?? 0;

        if (holding == null || owned < request.Quantity)
            throw new BusinessRuleException($"Quantité détenue insuffisante : {owned} en portefeuille, {request.Quantity} demandées.");

        var taxRate = MarketRules.TaxRate(resource.Rules, now, price);
        var proceeds = Math.Round(price * request.Quantity * (1 - taxRate), 2);

       
        var costBasis = 0m;  
        var maxProfit = 0m;  
        var remaining = request.Quantity;

        foreach (var lot in holding.Lots.OrderBy(lot => lot.BoughtAt))
        {
            if (remaining == 0)
                break;

            var taken = Math.Min(lot.Quantity, remaining);
            var bestPrice = BestPriceSince(resource, lot.BoughtAt);

            costBasis += taken * lot.BuyPrice;
            maxProfit += taken * (bestPrice - lot.BuyPrice);

            lot.Quantity -= taken;
            remaining -= taken;
        }

        holding.Lots.RemoveAll(lot => lot.Quantity == 0);
        if (holding.Lots.Count == 0)
            user.Portfolio.Remove(holding);

        user.Balance += proceeds;

        var profit = Math.Round(proceeds - costBasis, 2);

      
        decimal? efficiency = maxProfit > 0 ? Math.Round(profit / maxProfit, 4) : null;

        var transaction = NewTransaction(request, resource, TransactionType.Sell, price, now);
        transaction.Profit = profit;
        transaction.Efficiency = efficiency;
        return transaction;
    }

    private static void EnsureAllowedByRules(Resource resource, DateTime now, decimal price, TransactionType type, int quantity)
    {
        var allowed = type == TransactionType.Buy
            ? MarketRules.CanBuy(resource.Rules, now, price)
            : MarketRules.CanSell(resource.Rules, now, price);

        if (!allowed)
            throw new BusinessRuleException(type == TransactionType.Buy
                ? $"Achat de {resource.Name} bloqué par une règle de marché active."
                : $"Vente de {resource.Name} bloquée par une règle de marché active.");

        var maxQuantity = MarketRules.MaxQuantity(resource.Rules, now, price);
        if (maxQuantity.HasValue && quantity > maxQuantity.Value)
            throw new BusinessRuleException($"Quantité limitée à {maxQuantity} par transaction sur {resource.Name}.");
    }


    private static decimal BestPriceSince(Resource resource, DateTime since) =>
        resource.PriceHistory
            .Where(point => point.At >= since)
            .Select(point => point.Price)
            .Append(resource.CurrentPrice)
            .Max();

    private static Transaction NewTransaction(TradeRequest request, Resource resource, TransactionType type, decimal price, DateTime now) => new()
    {
        TransactionId = request.TransactionId,
        ResourceId = resource.Id,
        ResourceName = resource.Name,
        Type = type,
        Quantity = request.Quantity,
        ExecutedPrice = price,
        CreatedAt = now,
        ExecutedAt = now,
        Offline = false,
        Status = TransactionStatus.Accepted
    };

    private static TradeResponse ToResponse(Transaction transaction, decimal balance) => new(
        TransactionId: transaction.TransactionId,
        Status: transaction.Status,
        ExecutedPrice: transaction.ExecutedPrice,
        Profit: transaction.Profit,
        Efficiency: transaction.Efficiency,
        NewBalance: balance);
}