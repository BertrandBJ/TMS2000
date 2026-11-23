using TMS2000.Mobile.Local;
using TMS2000.Shared.DTO.Trades;

namespace TMS2000.Mobile.Services;

public record TradeResult(TradeResponse? ExecutedTrade, PendingAction? QueuedAction);
