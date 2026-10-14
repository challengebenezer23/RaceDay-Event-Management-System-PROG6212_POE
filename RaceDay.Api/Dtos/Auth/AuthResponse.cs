namespace RaceDay.Api.Dtos.Auth;

/// <summary>Response body for a successful register or login.</summary>
public class AuthResponse
{
    public int UserId { get; set; }
    public string Role { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string Message { get; set; }
}
