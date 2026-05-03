using System;
using Newtonsoft.Json;

namespace Networking.Omt.Dto.MarineSubscriptionService;

public class Chart
{
	public required string AppStoreId { get; init; }

	public required Uri CoverageAreaUrl { get; init; }

	public required string Description { get; init; }

	public required string DownloadPartNumber { get; init; }

	public required Uri[] Eulas { get; init; }

	public required string Name { get; init; }

	public required bool ProductEnabled { get; init; }

	public required string ScrId { get; init; }

	public required bool CompatibleDeviceRegistered { get; init; }

	[JsonConverter(typeof(ChartDateTimeConverter))]
	public required DateTime SubscriptionExpirationDate { get; init; }
}
