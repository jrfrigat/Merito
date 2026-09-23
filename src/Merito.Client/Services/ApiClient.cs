using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Merito.Shared;
using Merito.Shared.Contracts;

namespace Merito.Client.Services;

/// <summary>A refusal from the API, carrying the message the server wrote for the user.</summary>
public sealed class ApiException(HttpStatusCode status, string message) : Exception(message)
{
    /// <summary>HTTP status of the refusal; 0 when the server could not be reached.</summary>
    public HttpStatusCode Status { get; } = status;
}

/// <summary>Typed calls to the Merito API. Every failure surfaces as an <see cref="ApiException"/>.</summary>
public sealed class ApiClient(HttpClient http)
{
    private static MeritoJsonContext Json => MeritoJsonContext.Default;

    // Auth
    public Task<TokenResponse> RegisterAsync(RegisterRequest r) => Send(HttpMethod.Post, "api/auth/register", r, Json.RegisterRequest, Json.TokenResponse);
    public Task<TokenResponse> LoginAsync(LoginRequest r) => Send(HttpMethod.Post, "api/auth/login", r, Json.LoginRequest, Json.TokenResponse);
    public Task<MeResponse> GetMeAsync() => Get("api/me", Json.MeResponse);

    // Families
    public Task<MembershipDto> CreateFamilyAsync(CreateFamilyRequest r) => Send(HttpMethod.Post, "api/families", r, Json.CreateFamilyRequest, Json.MembershipDto);
    public Task<MembershipDto> JoinFamilyAsync(JoinFamilyRequest r) => Send(HttpMethod.Post, "api/families/join", r, Json.JoinFamilyRequest, Json.MembershipDto);
    public Task<FamilyDto> GetFamilyAsync(Guid f) => Get($"api/families/{f}", Json.FamilyDto);
    public Task RenameFamilyAsync(Guid f, RenameFamilyRequest r) => Send(HttpMethod.Put, $"api/families/{f}", r, Json.RenameFamilyRequest);
    public Task<DashboardDto> GetDashboardAsync(Guid f) => Get($"api/families/{f}/dashboard", Json.DashboardDto);
    public Task<InviteDto> CreateInviteAsync(Guid f, CreateInviteRequest r) => Send(HttpMethod.Post, $"api/families/{f}/invites", r, Json.CreateInviteRequest, Json.InviteDto);
    public Task<MemberDto> CreateChildAsync(Guid f, CreateChildRequest r) => Send(HttpMethod.Post, $"api/families/{f}/children", r, Json.CreateChildRequest, Json.MemberDto);
    public Task RemoveMemberAsync(Guid f, Guid m) => SendEmpty(HttpMethod.Delete, $"api/families/{f}/members/{m}");
    public Task ResetPasswordAsync(Guid f, Guid m, ResetPasswordRequest r) => Send(HttpMethod.Put, $"api/families/{f}/members/{m}/password", r, Json.ResetPasswordRequest);

    // Catalog
    public Task<List<TaskDto>> GetTasksAsync(Guid f) => Get($"api/families/{f}/tasks", Json.ListTaskDto);
    public Task<TaskDto> AddTaskAsync(Guid f, TaskRequest r) => Send(HttpMethod.Post, $"api/families/{f}/tasks", r, Json.TaskRequest, Json.TaskDto);
    public Task<TaskDto> UpdateTaskAsync(Guid f, Guid id, TaskRequest r) => Send(HttpMethod.Put, $"api/families/{f}/tasks/{id}", r, Json.TaskRequest, Json.TaskDto);
    public Task DeleteTaskAsync(Guid f, Guid id) => SendEmpty(HttpMethod.Delete, $"api/families/{f}/tasks/{id}");
    public Task<List<PenaltyDto>> GetPenaltiesAsync(Guid f) => Get($"api/families/{f}/penalties", Json.ListPenaltyDto);
    public Task<PenaltyDto> AddPenaltyAsync(Guid f, PenaltyRequest r) => Send(HttpMethod.Post, $"api/families/{f}/penalties", r, Json.PenaltyRequest, Json.PenaltyDto);
    public Task<PenaltyDto> UpdatePenaltyAsync(Guid f, Guid id, PenaltyRequest r) => Send(HttpMethod.Put, $"api/families/{f}/penalties/{id}", r, Json.PenaltyRequest, Json.PenaltyDto);
    public Task DeletePenaltyAsync(Guid f, Guid id) => SendEmpty(HttpMethod.Delete, $"api/families/{f}/penalties/{id}");
    public Task<List<RewardDto>> GetRewardsAsync(Guid f) => Get($"api/families/{f}/rewards", Json.ListRewardDto);
    public Task<RewardDto> AddRewardAsync(Guid f, RewardRequest r) => Send(HttpMethod.Post, $"api/families/{f}/rewards", r, Json.RewardRequest, Json.RewardDto);
    public Task<RewardDto> UpdateRewardAsync(Guid f, Guid id, RewardRequest r) => Send(HttpMethod.Put, $"api/families/{f}/rewards/{id}", r, Json.RewardRequest, Json.RewardDto);
    public Task DeleteRewardAsync(Guid f, Guid id) => SendEmpty(HttpMethod.Delete, $"api/families/{f}/rewards/{id}");

    // Submissions
    public Task<List<SubmissionDto>> GetSubmissionsAsync(Guid f, SubmissionStatus? status = null, int take = 50) =>
        Get($"api/families/{f}/submissions?take={take}" + (status is null ? "" : $"&status={status}"), Json.ListSubmissionDto);
    public Task<SubmissionDto> SubmitAsync(Guid f, SubmitRequest r) => Send(HttpMethod.Post, $"api/families/{f}/submissions", r, Json.SubmitRequest, Json.SubmissionDto);
    public Task WithdrawAsync(Guid f, Guid id) => SendEmpty(HttpMethod.Delete, $"api/families/{f}/submissions/{id}");
    public Task ApproveAsync(Guid f, Guid id, ApproveRequest r) => Send(HttpMethod.Post, $"api/families/{f}/submissions/{id}/approve", r, Json.ApproveRequest);
    public Task RejectAsync(Guid f, Guid id, RejectRequest r) => Send(HttpMethod.Post, $"api/families/{f}/submissions/{id}/reject", r, Json.RejectRequest);

    // Points
    public Task<List<TransactionDto>> GetTransactionsAsync(Guid f, Guid? childId = null, int take = 50) =>
        Get($"api/families/{f}/transactions?take={take}" + (childId is null ? "" : $"&childId={childId}"), Json.ListTransactionDto);
    public Task<BalanceDto> AdjustPointsAsync(Guid f, AdjustPointsRequest r) => Send(HttpMethod.Post, $"api/families/{f}/points/adjust", r, Json.AdjustPointsRequest, Json.BalanceDto);
    public Task<BalanceDto> ApplyPenaltyAsync(Guid f, ApplyPenaltyRequest r) => Send(HttpMethod.Post, $"api/families/{f}/points/penalty", r, Json.ApplyPenaltyRequest, Json.BalanceDto);

    // Shop
    public Task<List<PurchaseDto>> GetPurchasesAsync(Guid f, PurchaseStatus? status = null, int take = 50) =>
        Get($"api/families/{f}/purchases?take={take}" + (status is null ? "" : $"&status={status}"), Json.ListPurchaseDto);
    public Task<PurchaseDto> BuyAsync(Guid f, PurchaseRequest r) => Send(HttpMethod.Post, $"api/families/{f}/purchases", r, Json.PurchaseRequest, Json.PurchaseDto);
    public Task FulfillAsync(Guid f, Guid id) => SendEmpty(HttpMethod.Post, $"api/families/{f}/purchases/{id}/fulfill");
    public Task CancelPurchaseAsync(Guid f, Guid id) => SendEmpty(HttpMethod.Post, $"api/families/{f}/purchases/{id}/cancel");

    // Notifications
    public Task<List<NotificationDto>> GetNotificationsAsync(Guid f, int take = 50) =>
        Get($"api/families/{f}/notifications?take={take}", Json.ListNotificationDto);
    public Task<UnreadCountDto> GetUnreadNotificationCountAsync(Guid f) =>
        Get($"api/families/{f}/notifications/unread-count", Json.UnreadCountDto);
    public Task MarkNotificationReadAsync(Guid f, Guid id) =>
        SendEmpty(HttpMethod.Post, $"api/families/{f}/notifications/{id}/read");
    public Task MarkAllNotificationsReadAsync(Guid f) =>
        SendEmpty(HttpMethod.Post, $"api/families/{f}/notifications/read-all");
    public Task<WebPushPublicKeyDto> GetWebPushPublicKeyAsync(Guid f) =>
        Get($"api/families/{f}/notifications/push/public-key", Json.WebPushPublicKeyDto);
    public Task SubscribeWebPushAsync(Guid f, WebPushSubscriptionRequest request) =>
        Send(HttpMethod.Post, $"api/families/{f}/notifications/push/subscriptions", request, Json.WebPushSubscriptionRequest);
    public Task UnsubscribeWebPushAsync(Guid f, WebPushUnsubscribeRequest request) =>
        Send(HttpMethod.Post, $"api/families/{f}/notifications/push/unsubscribe", request, Json.WebPushUnsubscribeRequest);

    private async Task<TResult> Get<TResult>(string url, JsonTypeInfo<TResult> resultInfo)
    {
        using var response = await SendCore(HttpMethod.Get, url, null);
        return await ReadAsync(response, resultInfo);
    }

    private async Task<TResult> Send<TBody, TResult>(HttpMethod method, string url, TBody body,
        JsonTypeInfo<TBody> bodyInfo, JsonTypeInfo<TResult> resultInfo)
    {
        using var response = await SendCore(method, url, JsonContent.Create(body, bodyInfo));
        return await ReadAsync(response, resultInfo);
    }

    private async Task Send<TBody>(HttpMethod method, string url, TBody body, JsonTypeInfo<TBody> bodyInfo)
    {
        using var _ = await SendCore(method, url, JsonContent.Create(body, bodyInfo));
    }

    private async Task SendEmpty(HttpMethod method, string url)
    {
        using var _ = await SendCore(method, url, null);
    }

    private static async Task<TResult> ReadAsync<TResult>(HttpResponseMessage response, JsonTypeInfo<TResult> info) =>
        await response.Content.ReadFromJsonAsync(info)
        ?? throw new ApiException(response.StatusCode, "Сервер вернул пустой ответ.");

    private async Task<HttpResponseMessage> SendCore(HttpMethod method, string url, HttpContent? content)
    {
        using var request = new HttpRequestMessage(method, url) { Content = content };

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request);
        }
        catch (HttpRequestException)
        {
            throw new ApiException(0, "Нет связи с сервером. Проверьте интернет и попробуйте еще раз.");
        }

        if (response.IsSuccessStatusCode) return response;

        using (response)
        {
            throw new ApiException(response.StatusCode, await ReadMessageAsync(response));
        }
    }

    private static async Task<string> ReadMessageAsync(HttpResponseMessage response)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync(Json.ProblemMessage);
            var text = problem?.Detail ?? problem?.Title;
            if (!string.IsNullOrWhiteSpace(text)) return text;
        }
        catch (JsonException) { }
        catch (NotSupportedException) { }

        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Сессия закончилась. Войдите снова.",
            HttpStatusCode.Forbidden => "Недостаточно прав для этого действия.",
            HttpStatusCode.NotFound => "Не найдено.",
            _ => $"Ошибка сервера ({(int)response.StatusCode}).",
        };
    }
}
