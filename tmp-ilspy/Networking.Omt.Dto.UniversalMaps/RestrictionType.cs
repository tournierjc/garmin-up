using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Networking.Omt.Dto.UniversalMaps;

[JsonConverter(typeof(StringEnumConverter))]
public enum RestrictionType
{
	Country
}
