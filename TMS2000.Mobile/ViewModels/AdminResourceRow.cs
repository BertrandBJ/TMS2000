using TMS2000.Shared.DTO.Admin;

namespace TMS2000.Mobile.ViewModels;

public record AdminResourceRow(
    AdminResourceDto Resource,
    string CurrentPriceDescription,
    string PriceModeDescription,
    string PriceBandDescription);
