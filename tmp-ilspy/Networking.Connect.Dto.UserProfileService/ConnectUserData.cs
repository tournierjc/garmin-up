using Newtonsoft.Json;

namespace Networking.Connect.Dto.UserProfileService;

public class ConnectUserData
{
	[JsonProperty("id")]
	public long Id { get; set; }

	[JsonProperty("userData")]
	public UserSettingsInfo? UserSettings { get; set; }

	[JsonProperty("userSleep")]
	public UserWellnessSleep? UserSleep { get; set; }
}
