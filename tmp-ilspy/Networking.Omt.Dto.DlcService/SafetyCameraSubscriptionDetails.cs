using System;

namespace Networking.Omt.Dto.DlcService;

public record SafetyCameraSubscriptionDetails(DateTime? ActivationDateTimeUtc, DateTime? ExpirationDateTimeUtc, SafetyCameraProductDetails ProductDetails, SubscriptionStatus Status, long UnitId);
