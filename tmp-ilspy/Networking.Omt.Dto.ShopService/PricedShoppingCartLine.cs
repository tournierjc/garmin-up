namespace Networking.Omt.Dto.ShopService;

public record PricedShoppingCartLine(string ProductPartNumber, int Quantity, ProductPrice ListPrice, ProductPrice? SalePrice, bool IsMsrp);
