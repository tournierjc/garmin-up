using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Networking.Connect.Dto;

[JsonConverter(typeof(StringEnumConverter))]
public enum Wrist
{
	[EnumMember(Value = "LEFT")]
	Left,
	[EnumMember(Value = "RIGHT")]
	Right
}
