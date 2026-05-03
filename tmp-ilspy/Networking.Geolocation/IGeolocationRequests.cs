using System.Collections.Immutable;
using System.Globalization;
using System.Threading.Tasks;

namespace Networking.Geolocation;

public interface IGeolocationRequests
{
	Task<string> GetGeolocationAsync();

	Task<ImmutableList<RegionInfo>> GetCountriesAsync();
}
