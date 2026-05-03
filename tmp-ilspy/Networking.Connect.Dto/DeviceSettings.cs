using System.Collections.Generic;
using Newtonsoft.Json;

namespace Networking.Connect.Dto;

public class DeviceSettings
{
	[JsonProperty("supportedLanguages")]
	public List<DeviceSettingsLanguage>? SupportedLanguages { get; set; }

	[JsonProperty("mountingSide")]
	public Wrist? Wrist { get; set; }

	[JsonProperty("screenOrientation")]
	public ScreenOrientation? ScreenOrientation { get; set; }

	internal DeviceSettings()
	{
	}

	public bool ShouldSerializeSupportedLanguages()
	{
		return false;
	}

	public bool ShouldSerializeWrist()
	{
		return Wrist.HasValue;
	}

	public bool ShouldSerializeScreenOrientation()
	{
		return ScreenOrientation.HasValue;
	}
}
