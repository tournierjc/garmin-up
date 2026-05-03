using Newtonsoft.Json;

namespace Networking.Connect.Dto.UserProfileService;

public class UserLocationResponse
{
	[JsonProperty("countryCode")]
	public string? CountryCode { get; set; }

	[JsonProperty("countryCodeVerified")]
	public bool CountryCodeVerified { get; set; }

	[JsonProperty("countryCodeVerifiedTimestamp")]
	public object? CountryCodeVerifiedTimestamp { get; set; }

	[JsonProperty("userProfileId")]
	public int UserProfileId { get; set; }
}
