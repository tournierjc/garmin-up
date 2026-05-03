using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Serializers;
using RestSharp.Serializers.NewtonsoftJson;

namespace Networking;

internal class NetworkingRestClient : IRestClient, IDisposable
{
	private readonly ILogger<NetworkingRestClient> _logger;

	private readonly RestClient _client;

	ReadOnlyRestClientOptions IRestClient.Options => _client.Options;

	RestSerializers IRestClient.Serializers => _client.Serializers;

	DefaultParameters IRestClient.DefaultParameters => _client.DefaultParameters;

	public NetworkingRestClient(ILogger<NetworkingRestClient> logger, string baseUrl)
	{
		_logger = logger;
		_client = new RestClient(baseUrl, delegate(RestClientOptions i)
		{
			i.Authenticator = this as IAuthenticator;
		}, null, delegate(SerializerConfig i)
		{
			i.UseNewtonsoftJson();
		});
	}

	public NetworkingRestClient(ILogger<NetworkingRestClient> logger, ConfigureRestClient? configureRestClient = null)
	{
		_logger = logger;
		_client = new RestClient(configureRestClient, null, delegate(SerializerConfig i)
		{
			i.UseNewtonsoftJson();
		});
	}

	void IDisposable.Dispose()
	{
		_client.Dispose();
	}

	public Uri BuildUri(RestRequest request)
	{
		return _client.BuildUri(request);
	}

	public virtual async Task<RestResponse> ExecuteAsync(RestRequest request, CancellationToken token = default(CancellationToken))
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		RestResponse restResponse = await _client.ExecuteAsync(request, token);
		stopwatch.Stop();
		LogRequest(request, restResponse, stopwatch.ElapsedMilliseconds);
		if (!restResponse.IsSuccessful)
		{
			throw new RestRequestException(restResponse);
		}
		return restResponse;
	}

	public virtual async Task<RestResponse<T>> ExecuteAsync<T>(RestRequest request, CancellationToken token = default(CancellationToken))
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		RestResponse<T> restResponse = await _client.ExecuteAsync<T>(request, token);
		stopwatch.Stop();
		LogRequest(request, restResponse, stopwatch.ElapsedMilliseconds);
		if (!restResponse.IsSuccessful)
		{
			throw new RestRequestException(restResponse);
		}
		return restResponse;
	}

	Task<Stream?> IRestClient.DownloadStreamAsync(RestRequest request, CancellationToken cancellationToken)
	{
		return _client.DownloadStreamAsync(request, cancellationToken);
	}

	protected void AddDefaultHeader(string name, string value)
	{
		_client.AddDefaultHeader(name, value);
	}

	private void LogRequest(RestRequest request, RestResponse response, long durationMs)
	{
		bool flag = false;
		if (request is CustomLoggingRestRequest customLoggingRestRequest)
		{
			if (customLoggingRestRequest.CensorRequest)
			{
				return;
			}
			flag = customLoggingRestRequest.CensorResponse;
		}
		var value = new
		{
			statusCode = response.StatusCode,
			content = (flag ? string.Empty : response.Content),
			headers = response.Headers,
			responseUri = response.ResponseUri,
			errorMessage = response.ErrorMessage
		};
		string message = $"Request completed in {durationMs} ms, Response: {JsonConvert.SerializeObject(value)}";
		if (response.IsSuccessful)
		{
			_logger.LogTrace(message);
		}
		else
		{
			_logger.LogWarning(message);
		}
	}
}
