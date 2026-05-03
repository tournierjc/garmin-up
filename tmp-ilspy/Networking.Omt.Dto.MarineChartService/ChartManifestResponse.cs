using System;

namespace Networking.Omt.Dto.MarineChartService;

public record ChartManifestResponse(bool? DoesAccountSupportNMaps, Uri[] EulaUrls, DownloadHosts DownloadHosts, ChartImage[] Images);
