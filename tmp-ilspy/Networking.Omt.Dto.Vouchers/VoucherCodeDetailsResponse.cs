using System.Collections.Generic;
using Newtonsoft.Json;

namespace Networking.Omt.Dto.Vouchers;

public class VoucherCodeDetailsResponse
{
	public string? Code { get; set; }

	[JsonProperty("products")]
	public List<RedeemableProduct>? Products { get; set; }

	[JsonProperty("isRedeemed")]
	public bool IsRedeemed { get; set; }

	[JsonProperty("type")]
	public VoucherCodeType Type { get; set; }
}
