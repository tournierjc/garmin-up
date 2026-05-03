using Newtonsoft.Json;

namespace Networking.Omt.Dto.Vouchers;

public class RedeemableProduct
{
	[JsonProperty("partNumber")]
	public string? PartNumber { get; set; }

	[JsonProperty("name")]
	public string? Name { get; set; }

	[JsonProperty("shortDescription")]
	public string? ShortDescription { get; set; }

	[JsonProperty("graphicUrl")]
	public string? Thumbnail { get; set; }

	[JsonProperty("CoverageUrl")]
	public string? CoverageImage { get; set; }

	[JsonProperty("locale")]
	public string? Locale { get; set; }
}
