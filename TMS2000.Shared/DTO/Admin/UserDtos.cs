using TMS2000.Shared.DTO.Players;

namespace TMS2000.Shared.DTO.Admin;

public record UsersResponse(List<UserSummaryDto> Users);

public record UserSummaryDto(
    string Id,
    string Email,
    decimal Balance);

public record UserDetailResponse(
    string Id,
    string Email,
    string Role,
    decimal Balance,
    List<HoldingDto> Portfolio,          // avec le détail des lots
    List<TransactionDto> Transactions);  // les plus récentes en premier