using System.Runtime.Serialization;
using Networking.Converters;
using Newtonsoft.Json;

namespace Networking.Omt.Dto.MarineChartService;

[JsonConverter(typeof(NullableStringEnumConverter))]
public enum ContentType
{
	Unknown,
	Device,
	[EnumMember(Value = "Content")]
	Chart
}
