using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RestSharp;

namespace Networking.GcsApi;

internal sealed class Ephemeris : IEphemeris
{
	private class EphemerisMtkResponse
	{
		[JsonProperty("gpsHours")]
		public long GpsHours { get; set; }

		[JsonProperty("data")]
		public string? DataString { get; set; }
	}

	private readonly GcsApiRestClient _restClient;

	private readonly uint _unitId;

	public uint UnitId { get; }

	public Ephemeris(GcsApiRestClient restClient, UnitId unitId)
	{
		_restClient = restClient;
		_unitId = unitId.Id;
	}

	public async Task<byte[]> GetMtkEphemerisAsync(string softwarePartNumber)
	{
		RestRequest request = new RestRequest("ephemeris/cpe/mtk/segments?segments=28");
		uint unitId = _unitId;
		request.AddHeader("X-Garmin-Unit-ID", unitId.ToString());
		request.AddHeader("X-Garmin-SW-Part-Number", softwarePartNumber);
		return (await _restClient.ExecuteAsync<List<EphemerisMtkResponse>>(request)).Data.SelectMany((EphemerisMtkResponse i) => Convert.FromBase64String(i.DataString)).ToArray();
	}

	public async Task<byte[]> GetSonyEphemerisAsync(string softwarePartNumber, string coverage)
	{
		RestRequest request = new RestRequest("ephemeris/cpe/sony?coverage=" + coverage);
		uint unitId = _unitId;
		request.AddHeader("X-Garmin-Unit-ID", unitId.ToString());
		request.AddHeader("X-Garmin-SW-Part-Number", softwarePartNumber);
		return (await _restClient.ExecuteAsync(request)).RawBytes;
	}
}
