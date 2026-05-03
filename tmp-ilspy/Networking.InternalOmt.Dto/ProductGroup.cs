using System.Collections.Generic;

namespace Networking.InternalOmt.Dto;

public record ProductGroup(string CouponLabelUrl, int GmaVersion, string GroupCode, string IsAoem, string Name, string ProductTypeUrl, List<string> RestrictedCountryCodes, string TypeCode);
