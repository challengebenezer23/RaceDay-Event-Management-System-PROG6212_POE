using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Dtos.Auth;
using RaceDay.Api.Dtos.Categories;
using RaceDay.Api.Dtos.Enrolments;
using RaceDay.Api.Dtos.Events;
using Xunit;

namespace RaceDay.Api.Tests;

/// <summary>
/// Integration tests for POST /api/enrolments, covering the Participant-only
/// happy path, the duplicate-enrolment conflict rule, and role enforcement
/// against Organisers.
/// </summary>
public class EnrolmentsTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory _factory;

    public EnrolmentsTests(RaceDayApiFactory factory)
    {
        _factory = factory;
    }

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

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = "TestPassword123"
        });

        loginResponse.EnsureSuccessStatusCode();

        return client;
    }

    /// <summary>Creates an event and one category under it as the given (already logged in) Organiser client.</summary>
    private static async Task<(int EventId, int CategoryId)> CreateEventWithCategoryAsync(HttpClient organiserClient)
    {
        var eventResponse = await organiserClient.PostAsJsonAsync("/api/events", new CreateEventRequest
        {
            EventName = $"Test Event {Guid.NewGuid():N}",
            Description = "An event created for enrolment testing.",
            EventDate = DateTime.UtcNow.AddMonths(1),
            Location = "Bloemfontein",
            Distance = 21.1m,
            EventType = "Run"
        });
        eventResponse.EnsureSuccessStatusCode();
        var raceEvent = await eventResponse.Content.ReadFromJsonAsync<EventResponse>();

        var categoryResponse = await organiserClient.PostAsJsonAsync(
            $"/api/events/{raceEvent!.EventID}/categories",
            new CreateCategoryRequest
            {
                CategoryName = "Open",
                MinAge = 18,
                MaxAge = 99,
                DistanceKM = 21.1m
            });
        categoryResponse.EnsureSuccessStatusCode();
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        return (raceEvent.EventID, category!.CategoryID);
    }

    [Fact]
    public async Task CreateEnrolment_AsParticipant_Returns201()
    {
        var organiserClient = await CreateAuthenticatedClientAsync("Organiser");
        var (eventId, categoryId) = await CreateEventWithCategoryAsync(organiserClient);

        var participantClient = await CreateAuthenticatedClientAsync("Participant");

        var response = await participantClient.PostAsJsonAsync("/api/enrolments", new CreateEnrolmentRequest
        {
            EventId = eventId,
            CategoryId = categoryId
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<EnrolmentResponse>();
        Assert.NotNull(body);
        Assert.Equal("Pending", body!.Status);
        Assert.Equal(eventId, body.EventID);
    }

    [Fact]
    public async Task CreateEnrolment_WhenAlreadyEnrolled_Returns409()
    {
        var organiserClient = await CreateAuthenticatedClientAsync("Organiser");
        var (eventId, categoryId) = await CreateEventWithCategoryAsync(organiserClient);

        var participantClient = await CreateAuthenticatedClientAsync("Participant");
        var request = new CreateEnrolmentRequest { EventId = eventId, CategoryId = categoryId };

        var firstResponse = await participantClient.PostAsJsonAsync("/api/enrolments", request);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var secondResponse = await participantClient.PostAsJsonAsync("/api/enrolments", request);

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task CreateEnrolment_AsOrganiser_Returns403()
    {
        var organiserClient = await CreateAuthenticatedClientAsync("Organiser");
        var (eventId, categoryId) = await CreateEventWithCategoryAsync(organiserClient);

        // The very same Organiser who owns the event now tries to enrol as
        // though they were a Participant - the endpoint is role-gated and
        // must reject this regardless of event ownership.
        var response = await organiserClient.PostAsJsonAsync("/api/enrolments", new CreateEnrolmentRequest
        {
            EventId = eventId,
            CategoryId = categoryId
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateEnrolment_WithUnknownEvent_Returns404()
    {
        var participantClient = await CreateAuthenticatedClientAsync("Participant");

        var response = await participantClient.PostAsJsonAsync("/api/enrolments", new CreateEnrolmentRequest
        {
            EventId = 999999,
            CategoryId = 1
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task WithdrawEnrolment_ByOwningParticipant_Returns200()
    {
        var organiserClient = await CreateAuthenticatedClientAsync("Organiser");
        var (eventId, categoryId) = await CreateEventWithCategoryAsync(organiserClient);

        var participantClient = await CreateAuthenticatedClientAsync("Participant");
        var createResponse = await participantClient.PostAsJsonAsync("/api/enrolments", new CreateEnrolmentRequest
        {
            EventId = eventId,
            CategoryId = categoryId
        });
        var enrolment = await createResponse.Content.ReadFromJsonAsync<EnrolmentResponse>();

        var withdrawResponse = await participantClient.DeleteAsync($"/api/enrolments/{enrolment!.EnrolmentID}");

        Assert.Equal(HttpStatusCode.OK, withdrawResponse.StatusCode);
    }

    [Fact]
    public async Task WithdrawEnrolment_ByAnotherParticipant_Returns403()
    {
        var organiserClient = await CreateAuthenticatedClientAsync("Organiser");
        var (eventId, categoryId) = await CreateEventWithCategoryAsync(organiserClient);

        var ownerParticipantClient = await CreateAuthenticatedClientAsync("Participant");
        var createResponse = await ownerParticipantClient.PostAsJsonAsync("/api/enrolments", new CreateEnrolmentRequest
        {
            EventId = eventId,
            CategoryId = categoryId
        });
        var enrolment = await createResponse.Content.ReadFromJsonAsync<EnrolmentResponse>();

        var otherParticipantClient = await CreateAuthenticatedClientAsync("Participant");
        var withdrawResponse = await otherParticipantClient.DeleteAsync($"/api/enrolments/{enrolment!.EnrolmentID}");

        Assert.Equal(HttpStatusCode.Forbidden, withdrawResponse.StatusCode);
    }
}
