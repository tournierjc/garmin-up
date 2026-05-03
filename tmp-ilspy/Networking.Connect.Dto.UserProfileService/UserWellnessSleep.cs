using Newtonsoft.Json;

namespace Networking.Connect.Dto.UserProfileService;

public class UserWellnessSleep
{
	[JsonProperty("sleepTime")]
	public int? SleepTime { get; set; }

	[JsonProperty("wakeTime")]
	public int? WakeTime { get; set; }
}
