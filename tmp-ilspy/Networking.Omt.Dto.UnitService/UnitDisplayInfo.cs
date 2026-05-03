using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Networking.Omt.Dto.UnitService;

public class UnitDisplayInfo
{
	public long UnitId { get; init; }

	public string? SerialNumber { get; init; }

	public Uri? GraphicUrl { get; init; }

	public ExpansionType ExpansionType { get; init; }

	public Uri? SupportPageUrl { get; init; }

	public MapStorageLocation MapStorageLocation { get; init; }

	public string? DeviceModel { get; init; }

	public bool NeedsUpdateTxt { get; init; }

	public string? UnitPartNumber { get; init; }

	public ConnectSupport ConnectSupport { get; init; }

	public MapUpdateType MapUpdateType { get; init; }

	public XmlRepair XmlRepair { get; init; }

	public Dictionary<string, string>? ApplicationBehaviors { get; init; }

	[JsonConverter(typeof(MarketSegmentJsonConverter))]
	public MarketSegment SegmentType { get; init; }
}
