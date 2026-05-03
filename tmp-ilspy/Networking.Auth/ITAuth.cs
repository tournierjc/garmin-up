using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Networking.Auth;

public record ITAuth
{
	public required Guid CustomerId { get; init; }

	public required string AccessToken { get; init; }

	public required string TokenType { get; init; }

	public required DateTime Expiration { get; init; }

	public required string RefreshToken { get; init; }

	public required DateTime RefreshTokenExpiration { get; init; }

	public required string Scope { get; init; }

	public string? MfaToken { get; init; }

	public DateTime? MfaExpirationTimestamp { get; init; }

	[CompilerGenerated]
	[SetsRequiredMembers]
	protected ITAuth(ITAuth original)
	{
		CustomerId = original.CustomerId;
		AccessToken = original.AccessToken;
		TokenType = original.TokenType;
		Expiration = original.Expiration;
		RefreshToken = original.RefreshToken;
		RefreshTokenExpiration = original.RefreshTokenExpiration;
		Scope = original.Scope;
		MfaToken = original.MfaToken;
		MfaExpirationTimestamp = original.MfaExpirationTimestamp;
	}

	public ITAuth()
	{
	}
}
