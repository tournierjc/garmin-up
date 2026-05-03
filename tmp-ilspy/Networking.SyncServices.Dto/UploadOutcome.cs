using System.Collections.Generic;

namespace Networking.SyncServices.Dto;

public record UploadOutcome(string ExternalId, List<UploadMessage> Messages, long? InternalId = null);
