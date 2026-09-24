using System.Text.Json;
using System.Text.Json.Serialization;

namespace Merito.Shared.Contracts;

/// <summary>
/// Source-generated serializers for every API contract, so the trimmed WebAssembly client never
/// depends on reflection to read or write them.
/// </summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(RegisterRequest))]
[JsonSerializable(typeof(LoginRequest))]
[JsonSerializable(typeof(RefreshRequest))]
[JsonSerializable(typeof(TokenResponse))]
[JsonSerializable(typeof(MeResponse))]
[JsonSerializable(typeof(MembershipDto))]
[JsonSerializable(typeof(CreateFamilyRequest))]
[JsonSerializable(typeof(RenameFamilyRequest))]
[JsonSerializable(typeof(JoinFamilyRequest))]
[JsonSerializable(typeof(CreateInviteRequest))]
[JsonSerializable(typeof(InviteDto))]
[JsonSerializable(typeof(CreateChildRequest))]
[JsonSerializable(typeof(ResetPasswordRequest))]
[JsonSerializable(typeof(FamilyDto))]
[JsonSerializable(typeof(MemberDto))]
[JsonSerializable(typeof(DashboardDto))]
[JsonSerializable(typeof(TaskRequest))]
[JsonSerializable(typeof(List<TaskDto>))]
[JsonSerializable(typeof(TaskDto))]
[JsonSerializable(typeof(PenaltyRequest))]
[JsonSerializable(typeof(List<PenaltyDto>))]
[JsonSerializable(typeof(PenaltyDto))]
[JsonSerializable(typeof(RewardRequest))]
[JsonSerializable(typeof(List<RewardDto>))]
[JsonSerializable(typeof(RewardDto))]
[JsonSerializable(typeof(SubmitRequest))]
[JsonSerializable(typeof(List<SubmissionDto>))]
[JsonSerializable(typeof(SubmissionDto))]
[JsonSerializable(typeof(ApproveRequest))]
[JsonSerializable(typeof(RejectRequest))]
[JsonSerializable(typeof(AdjustPointsRequest))]
[JsonSerializable(typeof(ApplyPenaltyRequest))]
[JsonSerializable(typeof(BalanceDto))]
[JsonSerializable(typeof(List<TransactionDto>))]
[JsonSerializable(typeof(PurchaseRequest))]
[JsonSerializable(typeof(List<PurchaseDto>))]
[JsonSerializable(typeof(PurchaseDto))]
[JsonSerializable(typeof(List<NotificationDto>))]
[JsonSerializable(typeof(NotificationDto))]
[JsonSerializable(typeof(NotificationPurchaseDto))]
[JsonSerializable(typeof(UnreadCountDto))]
[JsonSerializable(typeof(WebPushPublicKeyDto))]
[JsonSerializable(typeof(WebPushSubscriptionRequest))]
[JsonSerializable(typeof(WebPushUnsubscribeRequest))]
[JsonSerializable(typeof(ProblemMessage))]
[JsonSerializable(typeof(StoredTokens))]
public sealed partial class MeritoJsonContext : JsonSerializerContext;

/// <summary>The part of an RFC 7807 problem response the client shows to the user.</summary>
/// <param name="Title">Short message.</param>
/// <param name="Detail">Longer message, when present.</param>
public sealed record ProblemMessage(string? Title, string? Detail);

/// <summary>Tokens as the client keeps them between visits.</summary>
/// <param name="AccessToken">Bearer access token.</param>
/// <param name="RefreshToken">Refresh token.</param>
/// <param name="ExpiresAt">UTC moment the access token expires.</param>
public sealed record StoredTokens(string AccessToken, string RefreshToken, DateTime ExpiresAt);
