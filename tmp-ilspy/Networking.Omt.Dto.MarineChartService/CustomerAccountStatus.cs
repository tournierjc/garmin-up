using System.Runtime.Serialization;
using Networking.Converters;
using Newtonsoft.Json;

namespace Networking.Omt.Dto.MarineChartService;

[JsonConverter(typeof(NullableStringEnumConverter))]
public enum CustomerAccountStatus
{
	Normal,
	[EnumMember(Value = "OVER_REGISTRATION_LIMIT")]
	OverRegistrationLimit,
	[EnumMember(Value = "OVER_REGISTRATION_HISTORY_LIMIT")]
	OverRegistrationHistoryLimit,
	[EnumMember(Value = "NO_REGISTERED_DEVICES")]
	NoRegisteredDevices
}
