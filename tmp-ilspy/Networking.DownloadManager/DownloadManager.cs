using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Contrib.DuplicateRequestCollapser;
using RestSharp;
using RestSharp.Interceptors;
using Services.Analytics;
using Utilities;

namespace Networking.DownloadManager;

internal sealed class DownloadManager : IDownloadManager
{
	private record AnalyticReport
	{
		public string Id { get; }

		public string? PartNumber { get; }

		public long? DownloadableSize { get; }

		public string? DownloadableMd5 { get; }

		public DateTime StartTime { get; } = DateTime.UtcNow;

		public long? ExistingBytes { get; set; }

		public string? ExistingMd5 { get; set; }

		public long? DownloadedBytes { get; set; }

		public string? DownloadedMd5 { get; set; }

		public string? Source { get; set; }

		public List<HttpStatusCode> HttpStatusCodes { get; } = new List<HttpStatusCode>();

		public List<string> InnerExceptions { get; } = new List<string>();

		public string? Exception { get; set; }

		public bool Success { get; set; }

		public const double SamplePct = 0.2;

		public AnalyticReport(Downloadable downloadable)
		{
			Id = downloadable.Id;
			PartNumber = null;
			DownloadableSize = downloadable.Size;
			DownloadableMd5 = downloadable.Md5;
		}

		[CompilerGenerated]
		protected AnalyticReport(AnalyticReport original)
		{
			Id = original.Id;
			PartNumber = original.PartNumber;
			DownloadableSize = original.DownloadableSize;
			DownloadableMd5 = original.DownloadableMd5;
			StartTime = original.StartTime;
			ExistingBytes = original.ExistingBytes;
			ExistingMd5 = original.ExistingMd5;
			DownloadedBytes = original.DownloadedBytes;
			DownloadedMd5 = original.DownloadedMd5;
			Source = original.Source;
			HttpStatusCodes = original.HttpStatusCodes;
			InnerExceptions = original.InnerExceptions;
			Exception = original.Exception;
			Success = original.Success;
		}
	}

	private const int CheckBytesLength = 1048576;

	private static readonly IAsyncPolicy s_downloadPolicy;

	private readonly ILogger<DownloadManager> _logger;

	private readonly IAnalyticsProvider _analytics;

	private readonly IRestClient _restClient;

	private readonly IFileSystem _fileSystem;

	static DownloadManager()
	{
		s_downloadPolicy = Policy.WrapAsync(AsyncRequestCollapserPolicy.Create(), Policy.BulkheadAsync(2, int.MaxValue), Policy.Handle<PartialContentMismatchException>().RetryAsync(), Policy.Handle<ByteSizeMismatchException>().RetryAsync(), Policy.Handle<HashMismatchException>().RetryAsync(), Policy.Handle<ObjectDisposedException>().RetryAsync(), Policy.Handle<IOException>().WaitAndRetryAsync(5, (int _) => TimeSpan.FromSeconds(5.0)), Policy.Handle<DownloadManagerException>().WaitAndRetryAsync(3, (int i) => TimeSpan.FromSeconds(Math.Pow(2.0, i))));
	}

	public DownloadManager(ILogger<DownloadManager> logger, IAnalyticsProvider analytics, IRestClient restClient, IFileSystem fileSystem)
	{
		_logger = logger;
		_analytics = analytics;
		_restClient = restClient;
		_fileSystem = fileSystem;
	}

	public async Task DownloadFileAsync(Downloadable downloadable, CancellationToken cancellationToken)
	{
		_logger.LogInformation(downloadable.Id + " - Starting Download");
		_logger.LogInformation(downloadable.Id + " - Downloadable Destination: " + downloadable.Destination);
		string fullPath = Path.GetFullPath(downloadable.Destination);
		fullPath = fullPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
		fullPath = fullPath.ToLowerInvariant();
		List<Exception> policyExceptions = new List<Exception>();
		AnalyticReport report = new AnalyticReport(downloadable);
		Stream fileStream = null;
		try
		{
			string directoryName = _fileSystem.Path.GetDirectoryName(downloadable.Destination);
			_fileSystem.Directory.CreateDirectory(directoryName);
			await s_downloadPolicy.ExecuteAsync(async delegate
			{
				HttpResponseMessage httpResponse = null;
				try
				{
					if (fileStream == null)
					{
						fileStream = _fileSystem.File.Open(downloadable.Destination, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
						report.ExistingBytes = fileStream.Length;
						if (fileStream.Length == downloadable.Size)
						{
							string text = await GetMd5Async(fileStream);
							report.ExistingMd5 = text;
							if (string.Equals(downloadable.Md5, text, StringComparison.InvariantCultureIgnoreCase))
							{
								downloadable.Progress.SetExistingBytes(fileStream.Length);
								report.Success = true;
								return;
							}
							fileStream.SetLength(0L);
						}
						if (fileStream.Length > downloadable.Size || fileStream.Length < 1048576)
						{
							fileStream.SetLength(0L);
						}
					}
					CompatibilityInterceptor item = new CompatibilityInterceptor
					{
						OnAfterRequest = delegate(HttpResponseMessage i)
						{
							report.HttpStatusCodes.Add(i.StatusCode);
							httpResponse = i;
							_logger.LogInformation($"{downloadable.Id} - Request Status Code: {i.StatusCode}");
							return default(ValueTask);
						}
					};
					List<Interceptor> interceptors = new List<Interceptor> { item };
					RestRequest request = new RestRequest(downloadable.Uri)
					{
						Timeout = TimeSpan.FromMinutes(5.0),
						Interceptors = interceptors
					};
					request.AddHeader("Accept", "*/*");
					if (downloadable.Size > 1048576)
					{
						long num = fileStream.Length - 1048576;
						if (num > 0)
						{
							request.AddHeader("Range", $"bytes={num}-");
						}
					}
					using Stream stream2 = await _restClient.DownloadStreamAsync(request, cancellationToken);
					if (stream2 == null)
					{
						report.Source = "stream";
						_logger.LogWarning(downloadable.Id + " - Stream was null returning from restClient.");
						throw new DownloadManagerException(downloadable.Uri.AbsoluteUri, httpResponse?.StatusCode ?? ((HttpStatusCode)0));
					}
					HttpStatusCode? httpStatusCode = httpResponse?.StatusCode;
					if (!httpStatusCode.HasValue)
					{
						goto IL_0571;
					}
					HttpStatusCode valueOrDefault = httpStatusCode.GetValueOrDefault();
					if (valueOrDefault != HttpStatusCode.OK)
					{
						if (valueOrDefault != HttpStatusCode.PartialContent)
						{
							goto IL_0571;
						}
						if (!(await CompareStreamsAsync(fileStream, stream2)))
						{
							_logger.LogWarning(downloadable.Id + " - CompareStreams detected content mismatch, deleting partial download.");
							fileStream.SetLength(0L);
							throw new PartialContentMismatchException();
						}
					}
					_logger.LogInformation($"{downloadable.Id} - Beginning download at byte index {fileStream.Position}. ({httpResponse.StatusCode})");
					await stream2.CopyToAsync(fileStream, cancellationToken, downloadable.Progress);
					report.DownloadedBytes = fileStream.Length;
					if (downloadable.Size.HasValue && downloadable.Size != fileStream.Length)
					{
						_logger.LogWarning($"{downloadable.Id} - Byte comparison failed. Expected: {downloadable.Size} Actual: {fileStream.Length}");
						fileStream.SetLength(0L);
						throw new ByteSizeMismatchException();
					}
					string text2 = await GetMd5Async(fileStream);
					report.DownloadedMd5 = text2;
					if (!string.IsNullOrWhiteSpace(downloadable.Md5) && !string.Equals(downloadable.Md5, text2, StringComparison.InvariantCultureIgnoreCase))
					{
						_logger.LogWarning(downloadable.Id + " - MD5 comparison failed. Expected: " + downloadable.Md5 + " Actual: " + text2);
						fileStream.SetLength(0L);
						throw new HashMismatchException(downloadable.Md5, text2);
					}
					goto end_IL_0338;
					IL_0571:
					report.Source = "switch";
					_logger.LogWarning($"{downloadable.Id} - Throwing within default switch value in DownloadManager. Response StatusCode: {httpResponse?.StatusCode}");
					throw new DownloadManagerException(downloadable.Uri.AbsoluteUri, httpResponse?.StatusCode ?? ((HttpStatusCode)0));
					end_IL_0338:;
				}
				catch (Exception ex3)
				{
					policyExceptions.Add(ex3);
					report.InnerExceptions.Add(ex3.GetType().Name);
					_logger.LogWarning($"{downloadable.Id} - Retrying download after Exception: {ex3}");
					throw;
				}
				finally
				{
					httpResponse?.Dispose();
				}
			}, new Context(fullPath));
			_logger.LogInformation(downloadable.Id + " - Download Succeeded.");
			report.Success = true;
		}
		catch when (policyExceptions.Count > 1)
		{
			AggregateException ex = new AggregateException("All DownloadManager.DownloadFileAsync() retries failed.", policyExceptions);
			_logger.LogError(ex, downloadable.Id + " - Download Failed.");
			_analytics.TrackException(ex, true, new { downloadable.Id });
			report.Exception = "AggregateException";
			throw ex;
		}
		catch (Exception ex2)
		{
			_logger.LogError(ex2, downloadable.Id + " - Download Failed.");
			_analytics.TrackException(ex2, true, new { downloadable.Id });
			report.Exception = ex2.GetType().Name;
			throw;
		}
		finally
		{
			Stream stream = fileStream;
			if (stream != null && stream.Length == 0)
			{
				fileStream.Dispose();
				_fileSystem.File.Delete(downloadable.Destination);
			}
			fileStream?.Dispose();
			if (!report.Success)
			{
				_analytics.TrackEvent("DownloadManager", report);
			}
		}
	}

	private static async Task<string> GetMd5Async(Stream fileStream)
	{
		return await Task.Run(delegate
		{
			fileStream.Seek(0L, SeekOrigin.Begin);
			using MD5 mD = MD5.Create();
			return BitConverter.ToString(mD.ComputeHash(fileStream)).Replace("-", "").ToLower();
		}).ConfigureAwait(continueOnCapturedContext: false);
	}

	private static async Task<bool> CompareStreamsAsync(Stream fileStream, Stream httpStream)
	{
		int remoteBytesRead = 0;
		int localBytesRead = 0;
		int remoteBytesToRead = 1048576;
		int localBytesToRead = 1048576;
		byte[] remoteCheckBytes = new byte[1048576];
		byte[] localCheckBytes = new byte[1048576];
		CancellationTokenSource timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(1.0));
		while (remoteBytesToRead > 0)
		{
			int num = await httpStream.ReadAsync(remoteCheckBytes, remoteBytesRead, remoteBytesToRead, timeoutCts.Token);
			remoteBytesRead += num;
			remoteBytesToRead -= num;
		}
		fileStream.Seek(-1048576L, SeekOrigin.End);
		while (localBytesToRead > 0)
		{
			int num2 = fileStream.Read(localCheckBytes, localBytesRead, localBytesToRead);
			localBytesRead += num2;
			localBytesToRead -= num2;
		}
		return remoteCheckBytes.SequenceEqual(localCheckBytes);
	}
}
