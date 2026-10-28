using System.Net.Http.Json;
using Newtonsoft.Json.Linq;
using SoundShapesServer.Requests.Api;
using SoundShapesServer.Extensions;
using SoundShapesServer.Responses.Api.Framework;
using SoundShapesServer.Responses.Api.Responses.Levels;
using SoundShapesServer.Types;
using SoundShapesServer.Types.Authentication;
using SoundShapesServer.Types.Levels;
using SoundShapesServer.Types.Users;
using SoundShapesServerTests.Server;

namespace SoundShapesServerTests.Tests;

public class LevelTests: ServerTest
{
    [Test]
    public void PrivateLevelAccessRequiresOwnerOrModerator()
    {
        using TestContext context = GetServer();
        GameUser owner = context.CreateUser("private-owner");
        GameUser stranger = context.CreateUser("private-stranger");
        GameUser moderator = context.CreateUser("private-moderator");
        context.Database.SetUserPermissions(moderator, PermissionsType.Moderator);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        GameLevel level = new() { Id = "private-probe", Author = owner, Name = "Private",
            CreationDate = now, ModificationDate = now, Visibility = LevelVisibility.Private,
            UploadPlatform = PlatformType.Unknown };
        context.Database.AddLevel(level, true);

        Assert.That(level.HasUserAccess(null), Is.False);
        Assert.That(level.HasUserAccess(stranger), Is.False);
        Assert.That(level.HasUserAccess(owner), Is.True);
        Assert.That(level.HasUserAccess(moderator), Is.True);
    }

    [Test]
    public async Task PrivateLevelEndpointsCheckAccess()
    {
        using TestContext context = GetServer();
        GameUser owner = context.CreateUser("endpoint-owner");
        GameUser stranger = context.CreateUser("endpoint-stranger");
        GameUser moderator = context.CreateUser("endpoint-moderator");
        context.Database.SetUserPermissions(moderator, PermissionsType.Moderator);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        GameLevel level = new() { Id = "private-endpoint", Author = owner, Name = "Private",
            CreationDate = now, ModificationDate = now, Visibility = LevelVisibility.Private,
            UploadPlatform = PlatformType.Unknown };
        context.Database.AddLevel(level, true);
        string upload = $"/otg/~level:{level.Id}/~upload:{now.ToUnixTimeMilliseconds()}.get";
        string latest = $"/otg/~level:{level.Id}.latest";
        using HttpClient ownerClient = context.GetAuthenticatedClient(TokenType.GameAccess, owner);
        using HttpClient strangerClient = context.GetAuthenticatedClient(TokenType.GameAccess, stranger);
        using HttpClient moderatorClient = context.GetAuthenticatedClient(TokenType.GameAccess, moderator);

        Assert.That((await context.Http.GetAsync(upload)).StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NotFound));
        foreach (string path in new[] { upload, latest })
        {
            Assert.That((await strangerClient.GetAsync(path)).StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NotFound), path);
            Assert.That((await ownerClient.GetAsync(path)).StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK), path);
            Assert.That((await moderatorClient.GetAsync(path)).StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK), path);
        }
    }

    [Test]
    public async Task MalformedGameLevelRoutesReturnClientErrors()
    {
        using TestContext context = GetServer();
        GameUser user = context.CreateUser();
        using HttpClient client = context.GetAuthenticatedClient(TokenType.GameAccess, user);

        HttpResponseMessage response = await client.GetAsync("/otg/~level:missing-action");
        Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NotFound));

        using StringContent body = new("not multipart");
        response = await client.PostAsync("/otg/~level:*.create", body);
        Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.BadRequest));

        foreach (string path in new[] {
            $"/otg/~identity:{user.Id}/~follow:%2F~identity%3Amissing",
            $"/otg/~identity:{user.Id}/~like:%2F~level%3Amissing",
            $"/otg/~identity:{user.Id}/~queued:%2F~level%3Amissing" })
        {
            response = await client.GetAsync(path);
            Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NotFound), path);
        }
        using StringContent recordBody = new("score=1");
        response = await client.PostAsync($"/otg/~identity:{user.Id}/~record:%2F~level%3Amissing", recordBody);
        Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NotFound));
    }

    [Test]
    public async Task MalformedRecordDoesNotCreatePlay()
    {
        using TestContext context = GetServer();
        GameUser user = context.CreateUser();
        GameLevel level = context.CreateLevel(user);
        context.Database.Refresh();
        using HttpClient client = context.GetAuthenticatedClient(TokenType.GameAccess, user);
        foreach (string body in new[] { "score", "score=not-a-number", "deaths=99999999999999999999" })
        {
            using StringContent content = new(body);
            HttpResponseMessage response = await client.PostAsync(
                $"/otg/~identity:{user.Id}/~record:%2F~level%3A{level.Id}.post", content);
            Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.BadRequest), body);
        }
        context.Database.Refresh();
        Assert.That(level.PlaysCount, Is.Zero);
    }

    [Test]
    public Task LevelFilteringWorks()
    {
        using TestContext context = GetServer();
        
        GameUser user = context.CreateUser();
        
        GameLevel firstLevel = context.CreateLevel(user, "First Level", DateTimeOffset.UtcNow);
        context.CreateLevel(user, "Second Level", DateTimeOffset.UtcNow.AddDays(1));

        context.Database.Refresh();

        // Filtration
        IEnumerable<GameLevel> levels = context.Database.GetLevels(LevelOrderType.CreationDate, true, new LevelFilters(){Search = firstLevel.Name}, null);
        Assert.That(levels.Count(), Is.EqualTo(1), "Check if filtration works");

        // Ordering
        levels = context.Database.GetLevels(LevelOrderType.CreationDate, true, new LevelFilters(), null);
        Assert.That(levels.First().CreationDate, Is.GreaterThan(levels.Last().CreationDate), "Check if ordering works");
        
        levels = context.Database.GetLevels(LevelOrderType.CreationDate, false, new LevelFilters(), null);
        Assert.That(levels.First().CreationDate, Is.LessThan(levels.Last().CreationDate), "Check if descending toggle works");
        return Task.CompletedTask;
    }
    
    [Test]
    public async Task LevelFetchingWorks()
    {
        using TestContext context = GetServer();
        
        GameUser user = context.CreateUser();
        
        GameLevel firstLevel = context.CreateLevel(user, "First Level", DateTimeOffset.UtcNow);
        context.CreateLevel(user, "Second Level", DateTimeOffset.UtcNow.AddDays(1));

        context.Database.Refresh();
        
        using HttpClient client = context.GetAuthenticatedClient(TokenType.GameAccess, user);

        // Search
        string payload = $"/otg/~index:level.page?query=metadata.displayName:{firstLevel.Name}";
        JObject response = JObject.Parse(await client.GetStringAsync(payload));
        
        Assert.That(response.GetValue("items")?.ToArray().Length, Is.EqualTo(1), "Check if search filter works");

        JObject all = JObject.Parse(await client.GetStringAsync("/otg/~index:level.page?search=all"));
        Assert.That(all.GetValue("items")?.ToArray().Length, Is.EqualTo(2), "Search mode all includes both levels");
    }

    [Test]
    public async Task ApiLevelLikingAndQueuingWorks()
    {
        using TestContext context = GetServer();
        
        GameUser user = context.CreateUser();
        GameLevel level = context.CreateLevel(user);
        
        context.Database.Refresh();
        
        using HttpClient client = context.GetAuthenticatedClient(TokenType.ApiAccess, user);
        
        // Check Relation
        string relationPayload = $"/api/v1/levels/id/{level.Id}/relationWith/id/{user.Id}";
        ApiResponse<ApiLevelRelationResponse>? relationResponse = 
            await client.GetFromJsonAsync<ApiResponse<ApiLevelRelationResponse>>(relationPayload);
        Assert.That(relationResponse?.Data is { Liked: false, Queued:false }, "Get relation of level");
        
        // Like Level
        string payload = $"/api/v1/levels/id/{level.Id}/like";
        HttpResponseMessage response = await client.PostAsync(payload, null);
        Assert.That(response.IsSuccessStatusCode, "Like level");
        
        // Check if Level was Liked
        relationResponse = 
            await client.GetFromJsonAsync<ApiResponse<ApiLevelRelationResponse>>(relationPayload);
        Assert.That(relationResponse?.Data is { Liked: true, Queued:false }, "Get like status of level");
        
        // Unlike Level
        payload = $"/api/v1/levels/id/{level.Id}/unLike";
        response = await client.PostAsync(payload, null);
        Assert.That(response.IsSuccessStatusCode, "Remove like from level");
        
        // Check if Like was removed
        relationResponse = 
            await client.GetFromJsonAsync<ApiResponse<ApiLevelRelationResponse>>(relationPayload);
        Assert.That(relationResponse?.Data is { Liked: false, Queued:false }, "Check if like was removed from level");
        
        // Queueing
        payload = $"/api/v1/levels/id/{level.Id}/queue";
        response = await client.PostAsync(payload, null);
        Assert.That(response.IsSuccessStatusCode, "Queue level");
        
        // Check if Level got Queued
        relationResponse = 
            await client.GetFromJsonAsync<ApiResponse<ApiLevelRelationResponse>>(relationPayload);
        Assert.That(relationResponse?.Data is { Liked: false, Queued:true }, "Check if level was queued");
        
        payload = $"/api/v1/levels/id/{level.Id}/unQueue";
        response = await client.PostAsync(payload, null);
        Assert.That(response.IsSuccessStatusCode, "Remove queue from level");
        
        // Check if Level was Un-queued
        relationResponse = 
            await client.GetFromJsonAsync<ApiResponse<ApiLevelRelationResponse>>(relationPayload);
        Assert.That(relationResponse?.Data is { Liked: false, Queued:false }, "Check if queue was removed from level");
    }
    
    [Test]
    public async Task LevelLikingAndQueuingWorks()
    {
        using TestContext context = GetServer();
        
        GameUser user = context.CreateUser();
        GameLevel level = context.CreateLevel(user);
        
        context.Database.Refresh();
        
        using HttpClient client = context.GetAuthenticatedClient(TokenType.GameAccess, user);
        
        // Liking
        string payload = $"/otg/~identity:{user.Id}/~like:%2F~level%3A{level.Id}.put";
        HttpResponseMessage response = await client.GetAsync(payload);
        Assert.That(response.IsSuccessStatusCode, "Like level");
        
        payload = $"/otg/~identity:{user.Id}/~like:%2F~level%3A{level.Id}.get";
        response = await client.GetAsync(payload);
        Assert.That(response.IsSuccessStatusCode, "Get like status of level");
        
        payload = $"/otg/~identity:{user.Id}/~like:%2F~level%3A{level.Id}.delete";
        response = await client.GetAsync(payload);
        Assert.That(response.IsSuccessStatusCode, "Remove like from level");
        
        // Queueing
        
        // you can't queue levels through the game, hence this not being done through the HTTP client
        context.Database.QueueLevel(user, level, PlatformType.Unknown);
        
        payload = $"/otg/~identity:{user.Id}/~queued:%2F~level%3A{level.Id}.get";
        response = await client.GetAsync(payload);
        Assert.That(response.IsSuccessStatusCode, "Get queue status on level");
        
        payload = $"/otg/~identity:{user.Id}/~queued:%2F~level%3A{level.Id}.delete";
        response = await client.GetAsync(payload);
        Assert.That(response.IsSuccessStatusCode, "Remove queue from level");
    }

    [Test]
    public async Task ApiLevelManagingWorks()
    {
        using TestContext context = GetServer();
        
        GameUser firstUser = context.CreateUser();
        GameLevel firstLevel = context.CreateLevel(firstUser);
        
        GameUser secondUser = context.CreateUser();
        GameLevel secondLevel = context.CreateLevel(secondUser);
        
        context.Database.Refresh();
        
        using HttpClient client = context.GetAuthenticatedClient(TokenType.ApiAccess, firstUser);
        
        // Updating Metadata
        string payload = $"/api/v1/levels/id/{firstLevel.Id}/edit";
        ApiLevelRequest body = new()
        {
            Name = "Updated Level Name"
        };
        HttpResponseMessage response = await client.PostAsJsonAsync(payload, body);
        Assert.That(response.IsSuccessStatusCode, "Edit level");
        
        // Try changing metadata of other user's level
        payload = $"/api/v1/levels/id/{secondLevel.Id}/edit";
        body = new()
        {
            Name = "Updated Level Name"
        };
        response = await client.PostAsJsonAsync(payload, body);
        Assert.That(!response.IsSuccessStatusCode, "Edit other user's level");
        
        // Removing Level
        payload = $"/api/v1/levels/id/{firstLevel.Id}";
        response = await client.DeleteAsync(payload);
        Assert.That(response.IsSuccessStatusCode, "Remove level");
        
        // Try Removing other user's level
        payload = $"/api/v1/levels/id/{secondLevel.Id}";
        response = await client.DeleteAsync(payload);
        Assert.That(!response.IsSuccessStatusCode, "Remove other user's level");
    }
}
