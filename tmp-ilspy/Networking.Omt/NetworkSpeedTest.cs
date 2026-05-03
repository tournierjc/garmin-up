using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using RestSharp;
using Utilities;

namespace Networking.Omt;

internal class NetworkSpeedTest : INetworkSpeedTest
{
	private readonly GarminEnvironment _garminEnvironment;

	public NetworkSpeedTest(GarminEnvironment garminEnvironment)
	{
		_garminEnvironment = garminEnvironment;
	}

	public async Task<double> RunDownloadSpeedTestAsync()
	{
		string text = ((_garminEnvironment != GarminEnvironment.China) ? "https://omtmapupdate.garmin.com" : "https://omtmapupdate.garmin.cn/");
		RestClient restClient = new RestClient(text);
		RestRequest request = new RestRequest(new Uri(new Uri(text), "omt/express/test/0.0/e1c59814de63633595be69383147c08b/speedtest.img").GetTokenized().PathAndQuery)
		{
			Timeout = TimeSpan.FromMinutes(5.0)
		};
		Dictionary<NetworkInterface, long> interfacesToSpeeds = NetworkInterface.GetAllNetworkInterfaces().ToDictionary((NetworkInterface i) => i, (NetworkInterface i) => i.GetIPv4Statistics().BytesReceived);
		Stopwatch sw = Stopwatch.StartNew();
		using Stream stream = await restClient.DownloadStreamAsync(request);
		await stream.CopyToAsync(Stream.Null);
		sw.Stop();
		return (double)interfacesToSpeeds.Select<KeyValuePair<NetworkInterface, long>, long>((KeyValuePair<NetworkInterface, long> i) => i.Key.GetIPv4Statistics().BytesReceived - i.Value).Max() / sw.Elapsed.TotalSeconds;
	}
}
