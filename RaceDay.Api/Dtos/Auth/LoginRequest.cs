using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos.Auth;

/// <summary>Request body for POST /api/auth/login.</summary>
public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; }

    [Required]
    public string Password { get; set; }
}
