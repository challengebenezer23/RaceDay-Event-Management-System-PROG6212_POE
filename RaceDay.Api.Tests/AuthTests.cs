using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Dtos.Auth;
using Xunit;

namespace RaceDay.Api.Tests;

/// <summary>
/// Integration tests for POST /api/auth/register and POST /api/auth/login.
/// Each test class owns its own RaceDayApiFactory (and therefore its own
/// isolated InMemory database), so tests never see data left behind by
/// another test class.
/// </summary>
public class AuthTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory _factory;

    public AuthTests(RaceDayApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_WithValidOrganiserDetails_Returns201AndAccount()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FullName = "Naledi Khumalo",
            Email = "naledi.khumalo@example.com",
            Password = "SecurePass123",
            Role = "Organiser",
            OrganisationName = "Joburg Road Runners"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(body);
        Assert.Equal("Organiser", body!.Role);
        Assert.True(body.UserId > 0);
        Assert.Equal("naledi.khumalo@example.com", body.Email);
    }

    [Fact]
    public async Task Register_WithValidParticipantDetails_Returns201AndAccount()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FullName = "Sipho Dlamini",
            Email = "sipho.dlamini@example.com",
            Password = "SecurePass123",
            Role = "Participant",
            DateOfBirth = new DateTime(1998, 4, 12)
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(body);
        Assert.Equal("Participant", body!.Role);
    }

    [Fact]
    public async Task Register_ParticipantWithoutDateOfBirth_Returns400()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FullName = "No Birthday",
            Email = "no.birthday@example.com",
            Password = "SecurePass123",
            Role = "Participant"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidRole_Returns400()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FullName = "Wrong Role",
            Email = "wrong.role@example.com",
            Password = "SecurePass123",
            Role = "Administrator"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409()
    {
        var client = _factory.CreateClient();

        var request = new RegisterRequest
        {
            FullName = "First Account",
            Email = "duplicate@example.com",
            Password = "SecurePass123",
            Role = "Organiser",
            OrganisationName = "Cape Town Athletics"
        };

        var firstResponse = await client.PostAsJsonAsync("/api/auth/register", request);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var secondRequest = new RegisterRequest
        {
            FullName = "Second Account",
            Email = "duplicate@example.com",
            Password = "AnotherPass456",
            Role = "Participant",
            DateOfBirth = new DateTime(2000, 1, 1)
        };
        var secondResponse = await client.PostAsJsonAsync("/api/auth/register", secondRequest);

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_Returns200AndStartsSession()
    {
        var client = _factory.CreateClient();

        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FullName = "Login Tester",
            Email = "login.tester@example.com",
            Password = "CorrectHorse1",
            Role = "Organiser",
            OrganisationName = "Durban Runners Club"
        });

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = "login.tester@example.com",
            Password = "CorrectHorse1"
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var body = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(body);
        Assert.Equal("Organiser", body!.Role);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var client = _factory.CreateClient();

        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FullName = "Bad Password Tester",
            Email = "bad.password@example.com",
            Password = "CorrectHorse1",
            Role = "Participant",
            DateOfBirth = new DateTime(1995, 6, 20)
        });

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = "bad.password@example.com",
            Password = "WrongPassword1"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_Returns401()
    {
        var client = _factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = "nobody.registered@example.com",
            Password = "WhateverPassword1"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }

    [Fact]
    public async Task Logout_WithoutAnActiveSession_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/auth/logout", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
