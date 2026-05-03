using Networking.Omt.Dto.ShopService;

namespace Networking.Omt.Dto.DlcService;

public record DlcProductsStatusResponse(SafetyCameraSubscriptionDetails[] SafetyCamerasSubscriptions, ProductDetails[] TrafficSubscriptions, ProductDetails[] DownloadableContents);
