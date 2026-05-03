using System;
using Newtonsoft.Json;

namespace Networking.Auth;

internal record ServicesTokenResponse([JsonProperty("access_token")] string AccessToken, [JsonProperty("token_type")] string TokenType, [JsonProperty("expires_in")][JsonConverter(typeof(SecondsUntilDateTimeConverter))] DateTime Expiration, [JsonProperty("refresh_token")] string RefreshToken, [JsonProperty("refresh_token_expires_in")][JsonConverter(typeof(SecondsUntilDateTimeConverter))] DateTime RefreshTokenExpiration, string Scope, Guid CustomerId, string? MfaToken, DateTime? MfaExpirationTimestamp);
