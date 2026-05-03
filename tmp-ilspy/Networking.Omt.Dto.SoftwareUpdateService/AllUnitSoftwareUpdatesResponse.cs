using System.Collections.Generic;
using Networking.Omt.Dto.DeviceSoftware;
using Newtonsoft.Json;

namespace Networking.Omt.Dto.SoftwareUpdateService;

public class AllUnitSoftwareUpdatesResponse
{
	public string? AccessLevel { get; set; }

	[JsonProperty("updates")]
	public List<DeviceSoftwareUpdate>? SoftwareUpdateOptions { get; set; }
}
