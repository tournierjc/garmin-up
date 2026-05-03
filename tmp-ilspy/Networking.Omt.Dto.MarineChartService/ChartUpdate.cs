using System;

namespace Networking.Omt.Dto.MarineChartService;

public record ChartUpdate(string SellableChartRegionId, string DownloadPartNumber, int MajorVersion, int MinorVersion, string GroupCode, bool IsFree, DateTime? FreeUpdateExpirationDate) : IChartInfo;
