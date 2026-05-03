using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Networking.Connect.Dto;

[JsonConverter(typeof(StringEnumConverter))]
public enum ScreenOrientation
{
	[EnumMember(Value = "LANDSCAPE")]
	Landscape,
	[EnumMember(Value = "PORTRAIT")]
	Portrait
}
