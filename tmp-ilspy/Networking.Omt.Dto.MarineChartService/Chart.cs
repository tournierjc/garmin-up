using System;

namespace Networking.Omt.Dto.MarineChartService;

public record Chart : IChartInfo
{
	public string SellableChartRegionId { get; }

	public string DownloadPartNumber { get; }

	public int MajorVersion { get; }

	public int MinorVersion { get; }

	public string GroupCode { get; }

	public string ProductName { get; }

	public string[] GmpTypes { get; }

	public bool IsCompatibleToAnyDeviceOnAccount { get; }

	public Uri? CoverageAreaImageUrl { get; }

	public string? ShortDescription { get; }

	public RedemptionStatus? StatusCode { get; }

	public ChartUpdate? Update { get; }

	public Chart(string downloadPartNumber, int majorVersion, int minorVersion, string sellableChartRegionId, string productName, string[] gmpTypes, string groupCode, bool? isCompatibleToAnyDeviceOnAccount, Uri? coverageAreaImageUrl, string? shortDescription, RedemptionStatus? statusCode, ChartUpdate? update)
	{
		DownloadPartNumber = downloadPartNumber;
		MajorVersion = majorVersion;
		MinorVersion = minorVersion;
		SellableChartRegionId = sellableChartRegionId;
		ProductName = productName;
		GmpTypes = gmpTypes;
		GroupCode = groupCode;
		IsCompatibleToAnyDeviceOnAccount = isCompatibleToAnyDeviceOnAccount == true;
		CoverageAreaImageUrl = coverageAreaImageUrl;
		ShortDescription = shortDescription;
		StatusCode = statusCode;
		Update = update;
	}
}
