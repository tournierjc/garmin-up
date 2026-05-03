using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using RestSharp;

namespace Networking.Geolocation;

internal sealed class GeolocationRequests : IGeolocationRequests
{
	private record GeolocationResponse(string Country);

	private record GeolocationCountries(string[] Countries);

	private static readonly RegionInfo WorldRegion = new RegionInfo("001");

	private readonly GeolocationRestClient _restClient;

	private readonly ICache? _cache;

	public GeolocationRequests(GeolocationRestClient restClient, ICache? cache = null)
	{
		_restClient = restClient;
		_cache = cache;
	}

	public async Task<string> GetGeolocationAsync()
	{
		GeolocationResponse geolocationResponse = _cache?.Get<GeolocationResponse>(new byte[1], TimeSpan.FromHours(1.0));
		if ((object)geolocationResponse != null)
		{
			return geolocationResponse.Country;
		}
		RestRequest request = new RestRequest("geolocation/whereami/akamai");
		RestResponse<GeolocationResponse> restResponse = await _restClient.ExecuteAsync<GeolocationResponse>(request);
		_cache?.Set(new byte[1], restResponse);
		return restResponse.Data.Country;
	}

	public async Task<ImmutableList<RegionInfo>> GetCountriesAsync()
	{
		GeolocationCountries geolocationCountries = _cache?.Get<GeolocationCountries>(new byte[1], TimeSpan.FromDays(7.0));
		if ((object)geolocationCountries == null)
		{
			GeolocationCountries geolocationCountries2 = default(GeolocationCountries);
			try
			{
				RestRequest request = new RestRequest("geolocation/countries");
				RestResponse<GeolocationCountries> restResponse = await _restClient.ExecuteAsync<GeolocationCountries>(request);
				_cache?.Set(new byte[1], restResponse);
				geolocationCountries = restResponse.Data;
			}
			catch (RestRequestException) when (((Func<bool>)delegate
			{
				// Could not convert BlockContainer to single expression
				geolocationCountries2 = _cache?.Get<GeolocationCountries>(new byte[1], TimeSpan.FromDays(28.0));
				return (object)geolocationCountries2 != null;
			}).Invoke())
			{
				geolocationCountries = geolocationCountries2;
			}
		}
		CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.SpecificCultures);
		return (from o in (from o in geolocationCountries.Countries.Join<string, CultureInfo, string, string>(cultures, (string o) => o, (CultureInfo i) => new RegionInfo(i.Name).TwoLetterISORegionName, (string o, CultureInfo i) => o, StringComparer.OrdinalIgnoreCase)
				select new RegionInfo(o)).Distinct()
			orderby o.DisplayName
			select o).ToImmutableList().Remove(WorldRegion);
	}
}
