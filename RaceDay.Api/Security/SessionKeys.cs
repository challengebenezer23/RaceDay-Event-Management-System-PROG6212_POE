namespace RaceDay.Api.Security;

/// <summary>
/// The well-known keys used to read and write the server-side session
/// (see Program.cs's AddSession registration and SessionAuthorizeAttribute).
/// </summary>
public static class SessionKeys
{
    public const string UserId = "UserId";
    public const string Role = "Role";
    public const string FullName = "FullName";
    public const string Email = "Email";
}

/// <summary>The two roles RaceDay supports, as session/role-check string constants.</summary>
public static class RaceDayRoles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}
