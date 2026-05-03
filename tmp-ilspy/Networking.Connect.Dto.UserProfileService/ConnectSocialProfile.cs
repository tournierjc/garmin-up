using Networking.Converters;
using Newtonsoft.Json;

namespace Networking.Connect.Dto.UserProfileService;

public class ConnectSocialProfile
{
	[JsonProperty("profileId")]
	public long ProfileId { get; set; }

	[JsonProperty("displayName")]
	public string? DisplayName { get; set; }

	[JsonProperty("fullName")]
	public string? FullName { get; set; }

	[JsonProperty("profileImageUrlLarge")]
	public string? ProfileImageUrlLarge { get; set; }

	[JsonProperty("profileImageUrlMedium")]
	public string? ProfileImageUrlMedium { get; set; }

	[JsonProperty("profileImageUrlSmall")]
	public string? ProfileImageUrlSmall { get; set; }

	[JsonProperty("location")]
	public string? Location { get; set; }

	[JsonProperty("userRoles")]
	[JsonConverter(typeof(NullFilteringCollectionConverter))]
	public ConnectProfileUserRole[]? UserRoles { get; set; }
}
