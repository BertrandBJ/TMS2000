using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TMS2000.Mobile.Helpers;
using TMS2000.Mobile.Services;
using TMS2000.Shared;
using TMS2000.Shared.DTO.Admin;

namespace TMS2000.Mobile.ViewModels;

public partial class AdminRuleEditorViewModel : ObservableObject
{
    private const string PriceConditionField = "price";
    private const int MinDurationHours = 1;
    private const int MaxDurationHours = 168;

    private readonly AdminService _adminService;

    public AdminRuleEditorViewModel(AdminService adminService)
    {
        _adminService = adminService;
        DateTime inOneHour = DateTime.Now.AddHours(1);
        StartDate = inOneHour.Date;
        StartTime = new TimeSpan(inOneHour.Hour, 0, 0);
    }

    public event EventHandler? RuleCreated;

    public List<string> OperatorOptions { get; } = new() { "<", "≤", ">", "≥" };

    public List<string> EffectOptions { get; } = new()
    {
        "Achat bloqué", "Vente bloquée", "Quantité max par transaction", "Taxe (%)"
    };

    [ObservableProperty] private List<SelectableResourceRow> _resourceRows = new();

    [ObservableProperty] private int _selectedOperatorIndex = (int)ConditionOperator.GreaterThan;

    [ObservableProperty] private string _conditionValueText = string.Empty;

    [ObservableProperty] private int _selectedEffectIndex = (int)EffectType.BuyBlocked;

    [ObservableProperty] private bool _isEffectValueNeeded;

    [ObservableProperty] private string _effectValueText = string.Empty;

    [ObservableProperty] private bool _startsLater;

    [ObservableProperty] private DateTime? _startDate;

    [ObservableProperty] private TimeSpan? _startTime;

    [ObservableProperty] private string _durationHoursText = "1";

    [ObservableProperty] private string? _errorMessage;

    [ObservableProperty] private bool _isBusy;

    partial void OnSelectedEffectIndexChanged(int value)
    {
        IsEffectValueNeeded = value is (int)EffectType.QuantityLimit or (int)EffectType.TransactionTax;
    }

    [RelayCommand]
    private async Task LoadResourcesAsync()
    {
        if (ResourceRows.Count > 0)
        {
            return;
        }

        ErrorMessage = null;
        try
        {
            AdminResourcesResponse resourcesResponse = await _adminService.GetResourcesAsync();
            ResourceRows = resourcesResponse.Resources
                .Select(resource => new SelectableResourceRow(resource.Id, resource.Name))
                .ToList();
        }
        catch (Exception exception) when (ApiErrorMessages.IsKnown(exception))
        {
            ErrorMessage = ApiErrorMessages.Describe(exception);
        }
    }

    [RelayCommand]
    private async Task CreateRuleAsync()
    {
        if (IsBusy)
        {
            return;
        }

        ErrorMessage = null;
        string? validationError = TryBuildRequest(out CreateRuleRequest? createRuleRequest);
        if (validationError is not null || createRuleRequest is null)
        {
            ErrorMessage = validationError;
            return;
        }

        IsBusy = true;
        try
        {
            await _adminService.CreateRuleAsync(createRuleRequest);
            RuleCreated?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception exception) when (ApiErrorMessages.IsKnown(exception))
        {
            ErrorMessage = ApiErrorMessages.Describe(exception);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private string? TryBuildRequest(out CreateRuleRequest? createRuleRequest)
    {
        createRuleRequest = null;

        List<string> selectedResourceIds = ResourceRows
            .Where(resourceRow => resourceRow.IsSelected)
            .Select(resourceRow => resourceRow.ResourceId)
            .ToList();
        if (selectedResourceIds.Count == 0)
        {
            return "Choisissez au moins une ressource.";
        }

        if (!DecimalParser.TryParse(ConditionValueText, out decimal conditionValue) || conditionValue <= 0)
        {
            return "Le prix de la condition doit être un nombre positif.";
        }

        decimal? effectValue = null;
        if (IsEffectValueNeeded)
        {
            if (!DecimalParser.TryParse(EffectValueText, out decimal parsedEffectValue) || parsedEffectValue <= 0)
            {
                return "La valeur de l'effet doit être un nombre positif.";
            }

            effectValue = parsedEffectValue;
        }

        if (!int.TryParse(DurationHoursText, out int durationHours)
            || durationHours < MinDurationHours || durationHours > MaxDurationHours)
        {
            return $"La durée doit être comprise entre {MinDurationHours} et {MaxDurationHours} heures.";
        }

        DateTime? startsAt = null;
        if (StartsLater)
        {
            if (StartDate is not DateTime chosenDate || StartTime is not TimeSpan chosenTime)
            {
                return "Choisissez une date et une heure de début.";
            }

            startsAt = DateTime.SpecifyKind(chosenDate.Date + chosenTime, DateTimeKind.Local).ToUniversalTime();
        }

        createRuleRequest = new CreateRuleRequest(
            ResourceIds: selectedResourceIds,
            Condition: new RuleConditionDto(PriceConditionField, (ConditionOperator)SelectedOperatorIndex, conditionValue),
            Effect: new RuleEffectDto((EffectType)SelectedEffectIndex, effectValue),
            StartsAt: startsAt,
            DurationHours: durationHours);

        return null;
    }
}
