using System;
using Networking.Omt.Dto.UnitService;
using Newtonsoft.Json;

namespace Networking.Omt.Dto.MarineChartService;

public record GmaContent(string Gma, string Identifier, string Name, Uri? GraphicUrl, [JsonConverter(typeof(MarketSegmentJsonConverter))] MarketSegment Segment, ContentType Type = ContentType.Unknown);
