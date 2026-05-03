using System;
using Newtonsoft.Json;

namespace Networking.Auth;

internal record DITokenResponse(string Jti, [JsonProperty("access_token")] string AccessToken, [JsonProperty("token_type")] string TokenType, [JsonProperty("expires_in")][JsonConverter(typeof(SecondsUntilDateTimeConverter))] DateTime Expiration, [JsonProperty("refresh_token")] string RefreshToken, [JsonProperty("refresh_token_expires_in")][JsonConverter(typeof(SecondsUntilDateTimeConverter))] DateTime RefreshTokenExpiration, string Scope, [JsonProperty("mfa_token")] string? MfaToken, [JsonProperty("mfa_expiration_timestamp")] DateTime? MfaExpirationTimestamp);
