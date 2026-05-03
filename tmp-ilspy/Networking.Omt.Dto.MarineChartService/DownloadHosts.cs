using System;

namespace Networking.Omt.Dto.MarineChartService;

public record DownloadHosts(Uri ForegroundPrimaryHost, Uri BackgroundPrimaryHost, Uri[] FailoverHosts);
