using TMS2000.Mobile.API;
using TMS2000.Shared.DTO.Admin;

namespace TMS2000.Mobile.Services;

public class AdminService
{
    private const string UsersRoute = "api/admin/users";
    private const string RulesRoute = "api/admin/rules";
    private const string ResourcesRoute = "api/admin/resources";

    private readonly ApiClient _apiClient;

    public AdminService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public Task<UsersResponse> GetUsersAsync() =>
        _apiClient.GetAsync<UsersResponse>(UsersRoute);

    public Task<UserDetailResponse> GetUserAsync(string userId) =>
        _apiClient.GetAsync<UserDetailResponse>($"{UsersRoute}/{Uri.EscapeDataString(userId)}");

    public Task<RulesResponse> GetRulesAsync() =>
        _apiClient.GetAsync<RulesResponse>(RulesRoute);

    public Task<CreateRuleResponse> CreateRuleAsync(CreateRuleRequest createRuleRequest) =>
        _apiClient.PostAsync<CreateRuleRequest, CreateRuleResponse>(RulesRoute, createRuleRequest);

    public Task DeleteRuleAsync(string ruleId) =>
        _apiClient.DeleteAsync($"{RulesRoute}/{Uri.EscapeDataString(ruleId)}");

    public Task<AdminResourcesResponse> GetResourcesAsync() =>
        _apiClient.GetAsync<AdminResourcesResponse>(ResourcesRoute);

    public Task<AdminResourceDto> UpdatePriceBandAsync(string resourceId, UpdatePriceBandRequest updatePriceBandRequest) =>
        _apiClient.PutAsync<UpdatePriceBandRequest, AdminResourceDto>(
            $"{ResourcesRoute}/{Uri.EscapeDataString(resourceId)}/price-band", updatePriceBandRequest);
}
