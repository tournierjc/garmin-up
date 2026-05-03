using System;
using System.Net;

namespace Networking.SyncServices.Dto;

public record UploadResponse(HttpStatusCode? ResponseCode = null, long? DelayTime = null, string? StatusUrl = null, Uri? ActivityUploadLocation = null, int? ActivityUploadDelay = null, UploadDetailedImportResult? DetailedImportResult = null);
