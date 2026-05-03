using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Networking.Omt.Dto.UnitService;

internal sealed class MarketSegmentJsonConverter : JsonConverter<MarketSegment>
{
	private static readonly IDictionary<string, MarketSegment> s_map = new Dictionary<string, MarketSegment>
	{
		["unspecified"] = MarketSegment.Unspecified,
		["golf"] = MarketSegment.Golf,
		["automotive"] = MarketSegment.Automotive,
		["smartwatch"] = MarketSegment.Fitness,
		["fitness"] = MarketSegment.Fitness,
		["outdoor"] = MarketSegment.Outdoor,
		["aviationportable"] = MarketSegment.AviationPortable,
		["marine"] = MarketSegment.Marine,
		["wellness"] = MarketSegment.Wellness,
		["aoem-kenwood"] = MarketSegment.Kenwood,
		["outdoor w/gc optional"] = MarketSegment.OutdoorWithConnect,
		["smartband"] = MarketSegment.Smartband,
		["actioncamera"] = MarketSegment.ActionCamera,
		["aoemhondaswonly"] = MarketSegment.HondaBeans,
		["aoemacuraswonly"] = MarketSegment.HondaBeans,
		["golfaccessory"] = MarketSegment.GolfAccessory,
		["bikeaccessory"] = MarketSegment.BikeAccessory,
		["wifiscale"] = MarketSegment.WiFiScale,
		["aoem-toyota"] = MarketSegment.Toyota,
		["vector"] = MarketSegment.Vector,
		["aviationfitness"] = MarketSegment.AviationFitness,
		["smartcollar"] = MarketSegment.SmartCollar,
		["fusionrv"] = MarketSegment.FusionRV,
		["vivomove"] = MarketSegment.Vivomove,
		["automotiveworldwide"] = MarketSegment.AutomotiveWorldWide,
		["aoem-carmax"] = MarketSegment.Carmax,
		["aoemhondagmf"] = MarketSegment.HondaGemini,
		["aoem-ctp"] = MarketSegment.AOEMCTP,
		["aoem-rhb"] = MarketSegment.AOEMRHB,
		["aoemsuzuki"] = MarketSegment.Suzuki,
		["baseballsensor"] = MarketSegment.Baseball,
		["webupdater"] = MarketSegment.WebUpdater,
		["aoem-vito"] = MarketSegment.AOEMVITO,
		["kidsmartwatch"] = MarketSegment.KidSmartwatch,
		["outdoorbasic"] = MarketSegment.OutdoorBasic
	};

	public override MarketSegment ReadJson(JsonReader reader, Type objectType, MarketSegment existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
		string key = (reader.Value as string)?.ToLowerInvariant() ?? string.Empty;
		if (s_map.TryGetValue(key, out var value))
		{
			return value;
		}
		return MarketSegment.Unknown;
	}

	public override void WriteJson(JsonWriter writer, MarketSegment value, JsonSerializer serializer)
	{
		serializer.Serialize(writer, s_map.Single<KeyValuePair<string, MarketSegment>>((KeyValuePair<string, MarketSegment> i) => i.Value == value).Key);
	}
}
