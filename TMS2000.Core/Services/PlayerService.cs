using TMS2000.Core.Exceptions;
using TMS2000.Core.Mappers;
using TMS2000.Core.Repositories;
using TMS2000.Shared.DTO.Players;

namespace TMS2000.Core.Services;

public class PlayerService
{
    private readonly UserRepository _userRepository;
    private readonly ResourceRepository _resourceRepository;

    public PlayerService(UserRepository userRepository, ResourceRepository resourceRepository)
    {
        _userRepository = userRepository;
        _resourceRepository = resourceRepository;
    }

    public async Task<MeResponse> GetMeAsync(string userId)
    {
        var user = await _userRepository.GetByIdAsync(userId)
                   ?? throw new NotFoundException("Joueur introuvable.");

    
        var resources = await _resourceRepository.GetAllWithoutHistoryAsync();
        var currentPrices = resources.ToDictionary(r => r.Id, r => r.CurrentPrice);

        var portfolio = user.Portfolio.Select(holding =>
        {
            var quantity = holding.Lots.Sum(lot => lot.Quantity);
            var totalCost = holding.Lots.Sum(lot => lot.Quantity * lot.BuyPrice);
            var currentValue = Math.Round(quantity * currentPrices.GetValueOrDefault(holding.ResourceId), 2);

            return new PortfolioItemDto(
                ResourceId: holding.ResourceId,
                ResourceName: holding.ResourceName,
                Quantity: quantity,
                AverageCost: quantity > 0 ? Math.Round(totalCost / quantity, 2) : 0m,
                CurrentValue: currentValue,
                UnrealizedProfit: Math.Round(currentValue - totalCost, 2));
        }).ToList();

        var recentTransactions = Enumerable.Reverse(user.Transactions)
            .Take(GameSettings.RecentTransactionsCount)
            .Select(PlayerMapper.ToTransactionDto)
            .ToList();

        return new MeResponse(user.Email, user.Balance, portfolio, recentTransactions);
    }
}