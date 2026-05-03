using System;

namespace Networking.Omt.Dto.ShopService;

public record OrderDetails(Guid CustomerGuid, string OrderId, OrderStatus OrderStatus, Product[] Products);
