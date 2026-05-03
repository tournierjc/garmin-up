using System;
using Newtonsoft.Json;

namespace Networking.Connect.Dto.UserProfileService;

public record UserSettingsInfo
{
	[JsonProperty("gender")]
	public string? Gender { get; set; }

	[JsonProperty("weight")]
	public double? Weight { get; set; }

	[JsonProperty("height")]
	public double? Height { get; set; }

	[JsonProperty("timeFormat")]
	public string? TimeFormat { get; set; }

	[JsonProperty("birthDate")]
	public DateTime? Birthdate { get; set; }

	[JsonProperty("measurementSystem")]
	public string? MeasurementSystem { get; set; }

	[JsonProperty("activityLevel")]
	public string? ActivityLevel { get; set; }

	[JsonProperty("handedness")]
	public string? Handedness { get; set; }

	public bool ShouldSerializeHandedness()
	{
		return !string.IsNullOrWhiteSpace(Handedness);
	}
}
