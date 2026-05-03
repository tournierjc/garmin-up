using System;
using System.Collections.Generic;

namespace Networking.Omt.Dto.ShopService;

public record ProductPriceRequest(string Locale, Product Product, IList<string>? PromotionCodes = null)
{
	public Guid? CustomerGuid { get; internal init; }
}
