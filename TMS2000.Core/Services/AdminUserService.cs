using TMS2000.Core.Exceptions;
using TMS2000.Core.Mappers;
using TMS2000.Core.Repositories;
using TMS2000.Shared.DTO.Admin;

namespace TMS2000.Core.Services;

public class AdminUserService
{
    private readonly UserRepository _userRepository;

    public AdminUserService(UserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UsersResponse> GetPlayersAsync()
    {
        var players = await _userRepository.GetPlayersAsync();
        return new UsersResponse(players
            .Select(player => new UserSummaryDto(player.Id, player.Email, player.Balance))
            .ToList());
    }

    public async Task<UserDetailResponse> GetUserAsync(string userId)
    {
        var user = await _userRepository.GetByIdAsync(userId)
                   ?? throw new NotFoundException($"Joueur '{userId}' introuvable.");

        return new UserDetailResponse(
            Id: user.Id,
            Email: user.Email,
            Role: user.Role,
            Balance: user.Balance,
            Portfolio: user.Portfolio.Select(PlayerMapper.ToHoldingDto).ToList(),
            Transactions: Enumerable.Reverse(user.Transactions).Select(PlayerMapper.ToTransactionDto).ToList());
    }
}