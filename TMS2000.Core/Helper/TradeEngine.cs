using TMS2000.Core.Exceptions;
using TMS2000.Core.Models;
using TMS2000.Shared;

namespace TMS2000.Core.Helper;

// Applique un achat ou une vente à un joueur, à un prix et une date donnés.
// - Trade en ligne : prix actuel, maintenant.
// - Synchro offline : prix historique, date de l'action.
// Ne touche pas à la base : modifie le User en mémoire, le service se charge d'écrire.
// Toutes les vérifications ont lieu AVANT toute modification : une exception laisse le joueur intact.
public static class TradeEngine
{
    public static Transaction Execute(User user, Resource resource, string transactionId,
        TransactionType type, int quantity, decimal unitPrice, DateTime executedAt, bool offline)
    {
        EnsureAllowedByRules(resource, executedAt, unitPrice, type, quantity);
        var taxRate = MarketRules.TaxRate(resource.Rules, executedAt, unitPrice);

        var transaction = new Transaction
        {
            TransactionId = transactionId,
            ResourceId = resource.Id,
            ResourceName = resource.Name,
            Type = type,
            Quantity = quantity,
            ExecutedPrice = unitPrice,
            CreatedAt = executedAt,
            ExecutedAt = executedAt,
            Offline = offline,
            Status = TransactionStatus.Accepted
        };

        if (type == TransactionType.Buy)
        {
            ApplyBuy(user, resource, quantity, unitPrice, taxRate, executedAt);
        }
        else
        {
            var saleResult = ApplySell(user, resource, quantity, unitPrice, taxRate, executedAt);
            transaction.Profit = saleResult.Profit;
            transaction.Efficiency = saleResult.Efficiency;
        }

        return transaction;
    }

    private static void ApplyBuy(User user, Resource resource, int quantity, decimal unitPrice, decimal taxRate, DateTime boughtAt)
    {
        var totalCost = Math.Round(unitPrice * quantity * (1 + taxRate), 2);

        if (totalCost > user.Balance)
            throw new BusinessRuleException($"Solde insuffisant : {totalCost} nécessaires, {user.Balance} disponibles.");

        user.Balance -= totalCost;

        var holding = user.Portfolio.FirstOrDefault(h => h.ResourceId == resource.Id);
        if (holding == null)
        {
            holding = new Holding { ResourceId = resource.Id, ResourceName = resource.Name };
            user.Portfolio.Add(holding);
        }

        holding.Lots.Add(new Lot
        {
            Quantity = quantity,
            BuyPrice = Math.Round(totalCost / quantity, 4),
            BoughtAt = boughtAt
        });
    }

    private static (decimal Profit, decimal? Efficiency) ApplySell(User user, Resource resource, int quantity, decimal unitPrice, decimal taxRate, DateTime soldAt)
    {
        var holding = user.Portfolio.FirstOrDefault(h => h.ResourceId == resource.Id);

        // Seuls les lots achetés AVANT la vente peuvent être vendus (important pour le rejeu offline)
        var lotsOwnedAtSale = holding?.Lots
            .Where(lot => lot.BoughtAt <= soldAt)
            .OrderBy(lot => lot.BoughtAt)          // FIFO : les plus anciens d'abord
            .ToList() ?? new List<Lot>();

        var ownedQuantity = lotsOwnedAtSale.Sum(lot => lot.Quantity);
        if (holding == null || ownedQuantity < quantity)
            throw new BusinessRuleException($"Quantité détenue insuffisante : {ownedQuantity} en portefeuille, {quantity} demandées.");

        var saleProceeds = Math.Round(unitPrice * quantity * (1 - taxRate), 2);
        var costOfSoldUnits = 0m;
        var maxPossibleProfit = 0m;
        var remainingToSell = quantity;

        foreach (var lot in lotsOwnedAtSale)
        {
            if (remainingToSell == 0)
                break;

            var takenFromLot = Math.Min(lot.Quantity, remainingToSell);
            var bestPriceDuringHolding = BestPriceBetween(resource, lot.BoughtAt, soldAt, unitPrice);

            costOfSoldUnits += takenFromLot * lot.BuyPrice;
            maxPossibleProfit += takenFromLot * (bestPriceDuringHolding - lot.BuyPrice);

            lot.Quantity -= takenFromLot;
            remainingToSell -= takenFromLot;
        }

        holding.Lots.RemoveAll(lot => lot.Quantity == 0);
        if (holding.Lots.Count == 0)
            user.Portfolio.Remove(holding);

        user.Balance += saleProceeds;

        var profit = Math.Round(saleProceeds - costOfSoldUnits, 2);
        decimal? efficiency = maxPossibleProfit > 0 ? Math.Round(profit / maxPossibleProfit, 4) : null;

        return (profit, efficiency);
    }

    private static void EnsureAllowedByRules(Resource resource, DateTime at, decimal price, TransactionType type, int quantity)
    {
        var allowed = type == TransactionType.Buy
            ? MarketRules.CanBuy(resource.Rules, at, price)
            : MarketRules.CanSell(resource.Rules, at, price);

        if (!allowed)
            throw new BusinessRuleException(type == TransactionType.Buy
                ? $"Achat de {resource.Name} bloqué par une règle de marché active."
                : $"Vente de {resource.Name} bloquée par une règle de marché active.");

        var maxQuantity = MarketRules.MaxQuantity(resource.Rules, at, price);
        if (maxQuantity.HasValue && quantity > maxQuantity.Value)
            throw new BusinessRuleException($"Quantité limitée à {maxQuantity} par transaction sur {resource.Name}.");
    }

    // Meilleur prix atteint entre l'achat d'un lot et sa vente
    private static decimal BestPriceBetween(Resource resource, DateTime from, DateTime to, decimal priceAtSale) =>
        resource.PriceHistory
            .Where(point => point.At >= from && point.At <= to)
            .Select(point => point.Price)
            .Append(priceAtSale)
            .Max();
}