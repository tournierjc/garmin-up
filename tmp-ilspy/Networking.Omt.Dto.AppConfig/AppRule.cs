using Newtonsoft.Json;

namespace Networking.Omt.Dto.AppConfig;

public class AppRule
{
	[JsonProperty("name")]
	public string? Name { get; set; }

	[JsonProperty("value")]
	public string? Value { get; set; }

	[JsonProperty("dataType")]
	public string? DataType { get; set; }
}
