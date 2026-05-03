using System;

namespace Networking.Omt.Dto.MarineChartService;

public record ChartImage(ChartAddition[] Additions, ContentDeliverable ContentDeliverable, string PartNumber, Uri? PreviewUrl);
