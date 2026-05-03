using System.Runtime.Serialization;
using Networking.Converters;
using Newtonsoft.Json;

namespace Networking.Connect.Dto.UserProfileService;

[JsonConverter(typeof(NullableStringEnumConverter))]
public enum ConnectProfileUserRole
{
	[EnumMember(Value = "ROLE_OUTDOOR_USER")]
	OutdoorUser,
	[EnumMember(Value = "ROLE_CONNECTUSER")]
	ConnectUser,
	[EnumMember(Value = "ROLE_FITNESS_USER")]
	FitnessUser,
	[EnumMember(Value = "ROLE_WELLNESS_USER")]
	WellnessUser,
	[EnumMember(Value = "ROLE_PARTIALLY_SUSPENDED")]
	PartiallySuspended
}
