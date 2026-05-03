namespace Networking.Omt.Dto.ShopService;

public record ShellOrderResponse(string OrderId, string OrderToken, string CheckoutUrl);
