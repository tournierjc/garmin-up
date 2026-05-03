using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Networking.Omt.Dto.Vouchers;

[JsonConverter(typeof(StringEnumConverter))]
public enum VoucherCodeType
{
	[EnumMember(Value = "Unknown")]
	Unknown,
	[EnumMember(Value = "RetailMapUpdate")]
	RetailMapUpdate,
	[EnumMember(Value = "LifetimeMapUpdate")]
	LifetimeMapUpdate,
	[EnumMember(Value = "DownloadableContent")]
	Dlc,
	[EnumMember(Value = "VivofitjrDlc")]
	VivofitJrDlc,
	[EnumMember(Value = "Birdseye")]
	BirdsEye
}
