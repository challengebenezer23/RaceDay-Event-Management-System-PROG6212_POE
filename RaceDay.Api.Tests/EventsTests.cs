using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Dtos.Auth;
using RaceDay.Api.Dtos.Events;
using Xunit;

namespace RaceDay.Api.Tests;

/// <summary>
/// Integration tests for the Events endpoints, covering the public read
/// access described in the Part 1 API plan and the Organiser-only,
/// role-enforced write access.
/// </summary>
public class EventsTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory _factory;

    public EventsTests(RaceDayApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Registers a fresh Organiser or Participant and returns an HttpClient
    /// that is already logged in (WebApplicationFactoryClientOptions defaults
    /// to HandleCookies = true, so the session cookie set by /login persists
    /// across every subsequent request made with this client).
    /// </summary>
    private async Task<HttpClient> CreateAuthenticatedClientAsync(string role)
    {
        var client = _factory.CreateClient();
        var email = $"{role.ToLowerInvariant()}.{Guid.NewGuid():N}@example.com";

        var register = new RegisterRequest
        {
            FullName = $"Test {role}",
            Email = email,
            Password = "TestPassword123",
            Role = role,
            OrganisationName = role == "Organiser" ? "Test Athletics Club" : null,
            DateOfBirth = role == "Participant" ? new DateTime(1997, 3, 15) : null
        };

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", register);
        registerResponse.EnsureSuccessStatusCode();

        // Log in immediately so the server-side session is created and the session cookie
        // is stored in the HttpClient. WebApplicationFactory's test client preserves cookies.
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = "TestPassword123"
        });
        loginResponse.EnsureSuccessStatusCode();

        return client;
    }

    private static CreateEventRequest SampleEvent(string suffix) => new()
    {
        EventName = $"Sunrise 10K {suffix}",
        Description = "A scenic early-morning 10km road race.",
        EventDate = DateTime.UtcNow.AddMonths(2),
        Location = "Pretoria",
        Distance = 10.0m,
        EventType = "Run"
    };

    [Fact]
    public async Task GetEvents_IsPubliclyAccessible_Returns200()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_AsOrganiser_Returns201()
    {
        var organiserClient = await CreateAuthenticatedClientAsync("Organiser");

        var response = await organiserClient.PostAsJsonAsync("/api/events", SampleEvent("A"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<EventResponse>();
        Assert.NotNull(body);
        Assert.Equal("Run", body!.EventType);
        Assert.True(body.EventID > 0);
    }

    [Fact]
    public async Task CreateEvent_AsParticipant_Returns403()
    {
        var participantClient = await CreateAuthenticatedClientAsync("Participant");

        var response = await participantClient.PostAsJsonAsync("/api/events", SampleEvent("B"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WhenNotLoggedIn_Returns401()
    {
        var anonymousClient = _factory.CreateClient();

        var response = await anonymousClient.PostAsJsonAsync("/api/events", SampleEvent("C"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithInvalidEventType_Returns400()
    {
        var organiserClient = await CreateAuthenticatedClientAsync("Organiser");
        var invalidEvent = SampleEvent("D");
        invalidEvent.EventType = "Swim";

        var response = await organiserClient.PostAsJsonAsync("/api/events", invalidEvent);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_ByNonOwningOrganiser_Returns403()
    {
        var ownerClient = await CreateAuthenticatedClientAsync("Organiser");
        var createResponse = await ownerClient.PostAsJsonAsync("/api/events", SampleEvent("E"));
        var created = await createResponse.Content.ReadFromJsonAsync<EventResponse>();

        var otherOrganiserClient = await CreateAuthenticatedClientAsync("Organiser");
        var updateRequest = new UpdateEventRequest
        {
            EventName = "Hijacked Name",
            Description = "Should not be allowed.",
            EventDate = DateTime.UtcNow.AddMonths(3),
            Location = "Nowhere",
            Distance = 5.0m,
            EventType = "Run"
        };

        var updateResponse = await otherOrganiserClient.PutAsJsonAsync($"/api/events/{created!.EventID}", updateRequest);

        Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);
    }

    [Fact]
    public async Task GetEvent_WithUnknownId_Returns404()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/events/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
