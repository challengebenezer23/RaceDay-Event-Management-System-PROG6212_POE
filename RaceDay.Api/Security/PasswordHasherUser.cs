namespace RaceDay.Api.Security;

/// <summary>
/// Marker type for <see cref="Microsoft.AspNetCore.Identity.PasswordHasher{TUser}"/>.
/// RaceDay does not use ASP.NET Core Identity's full user model - Organisers and
/// Participants are plain EF Core entities - but PasswordHasher&lt;TUser&gt; is a
/// battle-tested, dependency-free way to hash and verify passwords (PBKDF2 under
/// the hood) without ever storing them in plain text. It only needs a generic
/// type parameter, not a real instance, so this empty class is sufficient.
/// </summary>
public class PasswordHasherUser
{
}
