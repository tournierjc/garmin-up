using System.Runtime.Serialization;

namespace Networking.Omt.Dto.MarineChartService;

public enum RedemptionStatus
{
	Success,
	[EnumMember(Value = "FAILED_NOT_FREE")]
	NotFree,
	[EnumMember(Value = "FAILED_ALREADY_OWN")]
	AlreadyOwned
}
