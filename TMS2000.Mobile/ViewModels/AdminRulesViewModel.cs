using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TMS2000.Mobile.Helpers;
using TMS2000.Mobile.Services;
using TMS2000.Shared.DTO.Admin;

namespace TMS2000.Mobile.ViewModels;

public partial class AdminRulesViewModel : ObservableObject
{
    private readonly AdminService _adminService;

    public AdminRulesViewModel(AdminService adminService)
    {
        _adminService = adminService;
    }

    [ObservableProperty] private List<AdminRuleRow> _ruleRows = new();

    [ObservableProperty] private bool _hasNoRules;

    [ObservableProperty] private string? _errorMessage;

    [ObservableProperty] private bool _isRefreshing;

    [RelayCommand]
    private async Task RefreshAsync()
    {
        ErrorMessage = null;
        try
        {
            Task<RulesResponse> rulesTask = _adminService.GetRulesAsync();
            Task<AdminResourcesResponse> resourcesTask = _adminService.GetResourcesAsync();
            await Task.WhenAll(rulesTask, resourcesTask);

            Dictionary<string, string> resourceNamesById = resourcesTask.Result.Resources
                .ToDictionary(resource => resource.Id, resource => resource.Name);
            DateTime now = DateTime.UtcNow;

            RuleRows = rulesTask.Result.Rules
                .OrderBy(rule => rule.StartsAt)
                .Select(rule => ToAdminRuleRow(rule, resourceNamesById, now))
                .ToList();
            HasNoRules = RuleRows.Count == 0;
        }
        catch (Exception exception) when (ApiErrorMessages.IsKnown(exception))
        {
            ErrorMessage = ApiErrorMessages.Describe(exception);
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private AdminRuleRow ToAdminRuleRow(RuleDto rule, Dictionary<string, string> resourceNamesById, DateTime now)
    {
        IEnumerable<string> resourceNames = rule.ResourceIds
            .Select(resourceId => resourceNamesById.GetValueOrDefault(resourceId, resourceId));

        return new AdminRuleRow(
            EffectDescription: RuleDescriber.DescribeEffect(rule.Effect),
            ConditionDescription: RuleDescriber.DescribeCondition(rule.Condition),
            ResourcesDescription: string.Join(", ", resourceNames),
            PeriodDescription: RuleDescriber.DescribePeriod(rule.StartsAt, rule.EndsAt, now),
            CreatedByDescription: $"Créée par {rule.CreatedBy}",
            DeleteCommand: new AsyncRelayCommand(() => DeleteRuleAsync(rule.RuleId)));
    }

    private async Task DeleteRuleAsync(string ruleId)
    {
        bool isConfirmed = await Shell.Current.DisplayAlert(
            "Supprimer la règle", "Cette règle sera retirée de toutes ses ressources.", "Supprimer", "Annuler");
        if (!isConfirmed)
        {
            return;
        }

        ErrorMessage = null;
        try
        {
            await _adminService.DeleteRuleAsync(ruleId);
            await RefreshAsync();
        }
        catch (Exception exception) when (ApiErrorMessages.IsKnown(exception))
        {
            ErrorMessage = ApiErrorMessages.Describe(exception);
        }
    }
}
