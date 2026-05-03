using System.Collections.Generic;

namespace Networking.SyncServices.Dto;

public record UploadDetailedImportResult(string FileName, List<UploadOutcome> Successes, List<UploadOutcome> Failures, long? UploadId = null, string? HealthUploadId = null, long? Owner = null, long? ProcessingTime = null, string? IpAddress = null);
