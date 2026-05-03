namespace Networking.Omt.Dto.MarineChartService;

public interface IChartInfo
{
	string SellableChartRegionId { get; }

	string DownloadPartNumber { get; }

	int MajorVersion { get; }

	int MinorVersion { get; }

	string GroupCode { get; }
}
