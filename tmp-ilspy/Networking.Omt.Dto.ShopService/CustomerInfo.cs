using System;

namespace Networking.Omt.Dto.ShopService;

public record CustomerInfo(string LocaleCode, Guid? CustomerGuid = null);
