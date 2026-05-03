using System;

namespace Networking.Omt.Dto.MarineChartService;

public record ContentDeliverable(Uri DownloadManifestUrl, Uri DownloadUrl, string Md5, long? SizeInBytes);
