using System;

namespace Networking.Auth;

public sealed class DIToken
{
	public string Jti { get; set; }

	public string AccessToken { get; set; }

	public string TokenType { get; set; }

	public DateTime Expiration { get; set; }

	public string RefreshToken { get; set; }

	public DateTime RefreshTokenExpiration { get; set; }

	public string Scope { get; set; }

	public string? MfaToken { get; set; }

	public DateTime? MfaExpirationTimestamp { get; set; }
}
