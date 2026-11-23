using CommunityToolkit.Mvvm.Input;

namespace TMS2000.Mobile.ViewModels;

public record AdminRuleRow(
    string EffectDescription,
    string ConditionDescription,
    string ResourcesDescription,
    string PeriodDescription,
    string CreatedByDescription,
    IAsyncRelayCommand DeleteCommand);
