using System;
using System.Collections.Generic;

namespace Networking.InternalOmt.Dto;

public record MapCareSubscription(string AcquisitionType, DateTime ActivatedOn, string ActivationStatus, DateTime ExpiresOn, uint Id, string PartNumber, uint UnitId, List<string> UpdateRegions);
