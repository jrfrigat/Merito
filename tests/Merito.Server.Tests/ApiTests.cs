using System.Net;
using System.Net.Http.Json;
using Merito.Server.Tests.Support;
using Merito.Shared;
using Merito.Shared.Contracts;

namespace Merito.Server.Tests;

public sealed class ApiTests
{
    [Fact]
    public async Task AFamilyRunsTheWholeLoopOverHttp()
    {
        await using var api = new MeritoApiFactory();
        var parent = await api.RegisterParentAsync("Papa");

        var family = await Post<MembershipDto>(parent, "/api/families", new CreateFamilyRequest("Ивановы", SeedExample: true));
        var root = $"/api/families/{family.FamilyId}";
        var child = await Post<MemberDto>(parent, root + "/children", new CreateChildRequest("Петя", "Petya", "kid123"));
        Assert.Equal("petya", child.Login);

        var kid = await api.LoginAsync("petya", "kid123");
        var me = await kid.GetFromJsonAsync<MeResponse>("/api/me");
        Assert.Equal(FamilyRole.Child, Assert.Single(me!.Families).Role);

        var task = (await kid.GetFromJsonAsync<List<TaskDto>>(root + "/tasks"))!.First();
        var submission = await Post<SubmissionDto>(kid, root + "/submissions", new SubmitRequest(task.Id, null, null));
        await Post(parent, $"{root}/submissions/{submission.Id}/approve", new ApproveRequest(task.Points + 10, null, null));

        var reward = (await kid.GetFromJsonAsync<List<RewardDto>>(root + "/rewards"))!.First(r => r.Cost <= task.Points + 10);
        var purchase = await Post<PurchaseDto>(kid, root + "/purchases", new PurchaseRequest(reward.Id));
        var notification = (await parent.GetFromJsonAsync<List<NotificationDto>>(root + "/notifications"))!
            .Single(n => n.Purchase?.Id == purchase.Id);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await kid.PostAsJsonAsync($"{root}/notifications/{notification.Id}/purchase/fulfill", new { })).StatusCode);
        await Post(parent, $"{root}/notifications/{notification.Id}/purchase/fulfill", new { });

        var resolvedNotification = (await parent.GetFromJsonAsync<List<NotificationDto>>(root + "/notifications"))!
            .Single(n => n.Id == notification.Id);
        Assert.Equal(PurchaseStatus.Fulfilled, resolvedNotification.Purchase!.Status);
        Assert.NotNull(resolvedNotification.ReadAt);

        var dashboard = await kid.GetFromJsonAsync<DashboardDto>(root + "/dashboard");
        Assert.Equal(task.Points + 10 - reward.Cost, dashboard!.Me.Balance);
        Assert.Equal(0, dashboard.PendingPurchases);
        Assert.Equal(2, dashboard.RecentTransactions.Count);
    }

    [Fact]
    public async Task AStrangerGetsNotFoundAndAChildGetsForbiddenOnParentActions()
    {
        await using var api = new MeritoApiFactory();
        var parent = await api.RegisterParentAsync("Owner");
        var family = await Post<MembershipDto>(parent, "/api/families", new CreateFamilyRequest("Семья", false));
        var root = $"/api/families/{family.FamilyId}";
        var child = await Post<MemberDto>(parent, root + "/children", new CreateChildRequest("Кид", "kid1", "kid123"));
        var kid = await api.LoginAsync("kid1", "kid123");
        var stranger = await api.RegisterParentAsync("Stranger");

        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync(root + "/tasks")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.PostAsJsonAsync(root + "/points/adjust", new AdjustPointsRequest(child.Id, 100, "x"))).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await kid.PostAsJsonAsync(root + "/points/adjust", new AdjustPointsRequest(child.Id, 100, "x"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await kid.PostAsJsonAsync(root + "/tasks", new TaskRequest("x", null, 1, null, TaskCategory.Daily, null))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await kid.PostAsJsonAsync(root + "/invites", new CreateInviteRequest(FamilyRole.Parent))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await parent.PostAsJsonAsync(root + "/submissions", new SubmitRequest(null, "x", null))).StatusCode);

        var anonymous = api.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(root + "/tasks")).StatusCode);
    }

    [Fact]
    public async Task RefuseMessagesReachTheClientAsProblemDetails()
    {
        await using var api = new MeritoApiFactory();
        var parent = await api.RegisterParentAsync("Mama");
        var response = await parent.PostAsJsonAsync("/api/families", new CreateFamilyRequest("", false));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Название семьи", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ARefreshTokenIssuesNewTokens()
    {
        await using var api = new MeritoApiFactory();
        var client = api.CreateClient();
        var register = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("R", "r@example.com", "secret1"));
        var tokens = (await register.Content.ReadFromJsonAsync<TokenResponse>())!;

        var refreshed = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(tokens.RefreshToken));
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        Assert.False(string.IsNullOrEmpty((await refreshed.Content.ReadFromJsonAsync<TokenResponse>())!.AccessToken));

        var wrong = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("r@example.com", "nope"));
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
    }

    private static async Task<T> Post<T>(HttpClient client, string url, object body)
    {
        var response = await client.PostAsJsonAsync(url, body);
        Assert.True(response.IsSuccessStatusCode, $"{url}: {(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private static async Task Post(HttpClient client, string url, object body)
    {
        var response = await client.PostAsJsonAsync(url, body);
        Assert.True(response.IsSuccessStatusCode, $"{url}: {(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}");
    }
}
