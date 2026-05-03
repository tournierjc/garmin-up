using System.Runtime.Serialization;
using Networking.Converters;
using Newtonsoft.Json;

namespace Networking.Omt.Dto.MarineAccountService;

[JsonConverter(typeof(NullableStringEnumConverter))]
public enum RegistrationStatus
{
	Unknown,
	[EnumMember(Value = "SUCCESS")]
	Success,
	[EnumMember(Value = "SUCCESS_NO_CONTENT_ADDED")]
	SuccessNoContentAdded,
	[EnumMember(Value = "FAILED_ALREADY_REGISTERED")]
	FailedAlreadyRegistered,
	[EnumMember(Value = "RMU_NO_PREDECESSOR")]
	RmuNoPredecessor
}
