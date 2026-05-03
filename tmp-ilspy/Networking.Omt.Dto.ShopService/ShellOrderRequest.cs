using System;
using System.Collections.Generic;

namespace Networking.Omt.Dto.ShopService;

public record ShellOrderRequest(string Locale, Product[] Products, IList<string>? PromotionCodes = null)
{
	public Guid? CustomerGuid { get; internal init; }
}
