using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using LiteDB;
using Microsoft.Extensions.Logging;
using Networking.Auth;
using Newtonsoft.Json;
using RestSharp;
using Services.Analytics;
using Utilities;

namespace Networking;

internal sealed class DiskCache : ICache, IDisposable
{
	private record CacheObject(string _id, DateTime CreationTime, string Json);

	private static readonly Regex s_collectionNameRegex = new Regex("[^a-zA-Z$_]", RegexOptions.Compiled);

	private readonly ILogger<DiskCache> _logger;

	private readonly LiteDatabase _cache;

	private readonly object _lock = new object();

	public DiskCache(ILogger<DiskCache> logger, IAnalyticsProvider analytics, GarminEnvironment garminEnvironment)
	{
		_logger = logger;
		string appDataPath = PathUtils.GetAppDataPath();
		string text = ((garminEnvironment == GarminEnvironment.Production) ? string.Empty : garminEnvironment.ToString());
		string text2 = Path.Combine(appDataPath, "requestcache" + text + ".db");
		string path = Path.Combine(appDataPath, "requestcache" + text + "-log.db");
		string text3 = Path.Combine(appDataPath, "requestcache.key");
		if (!File.Exists(text3))
		{
			File.Delete(text2);
			File.Delete(path);
		}
		try
		{
			_cache = new LiteDatabase($"Filename={text2};Password={GetOrGenerateKey(text3)}");
		}
		catch (Exception exception)
		{
			File.Delete(text3);
			_logger.LogWarning(exception, "Failed to initialize DiskCache instance.");
			analytics.TrackException(exception, true, "DiskCache");
			throw;
		}
	}

	public void Dispose()
	{
		lock (_lock)
		{
			try
			{
				_cache.Dispose();
			}
			catch
			{
			}
		}
	}

	public T? Get<T>(byte[] cacheKey, TimeSpan cacheLength) where T : class
	{
		string collectionName = GetCollectionName<T>();
		string text = BitConverter.ToString(cacheKey);
		BsonDocument bsonDocument = null;
		lock (_lock)
		{
			bsonDocument = _cache.GetCollection(collectionName).FindById(text);
		}
		if (bsonDocument == null)
		{
			return null;
		}
		try
		{
			CacheObject cacheObject = BsonMapper.Global.ToObject<CacheObject>(bsonDocument);
			if (cacheLength != TimeSpan.MaxValue && cacheObject.CreationTime.Add(cacheLength) < DateTime.Now)
			{
				return null;
			}
			return JsonConvert.DeserializeObject<T>(cacheObject.Json);
		}
		catch
		{
			lock (_lock)
			{
				_cache.GetCollection(collectionName).Delete(text);
			}
			return null;
		}
	}

	public void Set<T>(byte[] cacheKey, T obj) where T : class
	{
		try
		{
			Set<T>(cacheKey, JsonConvert.SerializeObject(obj));
		}
		catch (Exception exception)
		{
			_logger.LogError(exception, "Failed to set " + typeof(T).FullName + " into cache");
		}
	}

	public void Set<T>(byte[] cacheKey, RestResponse<T> response) where T : class
	{
		if (response.Content == null)
		{
			return;
		}
		try
		{
			Set<T>(cacheKey, response.Content);
		}
		catch (Exception exception)
		{
			_logger.LogError(exception, "Failed to set " + typeof(T).FullName + " into cache");
		}
	}

	public DeviceAuth Get(UnitId unitId)
	{
		lock (_lock)
		{
			return _cache.GetCollection<DeviceAuth>().FindById((long)unitId.Id) ?? new DeviceAuth
			{
				UnitId = unitId.Id
			};
		}
	}

	public void Set(DeviceAuth auth)
	{
		lock (_lock)
		{
			_cache.GetCollection<DeviceAuth>().Upsert(auth);
		}
	}

	public AccountAuth Get(CustomerGuid customerGuid)
	{
		lock (_lock)
		{
			return _cache.GetCollection<AccountAuth>().FindById(customerGuid.Guid) ?? new AccountAuth
			{
				CustomerId = customerGuid.Guid
			};
		}
	}

	public void Set(AccountAuth auth)
	{
		lock (_lock)
		{
			_cache.GetCollection<AccountAuth>().Upsert(auth);
		}
	}

	public void DeleteAuth(UnitId unitId)
	{
		lock (_lock)
		{
			_cache.GetCollection<DeviceAuth>().Delete((long)unitId.Id);
		}
	}

	public void DeleteAuth(CustomerGuid customerGuid)
	{
		lock (_lock)
		{
			_cache.GetCollection<AccountAuth>().Delete(customerGuid.Guid);
		}
	}

	private void Set<T>(byte[] cacheKey, string json) where T : class
	{
		try
		{
			string collectionName = GetCollectionName<T>();
			string text = BitConverter.ToString(cacheKey);
			CacheObject entity = new CacheObject(text, DateTime.Now, json);
			BsonDocument entity2 = BsonMapper.Global.ToDocument(entity);
			lock (_lock)
			{
				ILiteCollection<BsonDocument> collection = _cache.GetCollection(collectionName);
				try
				{
					collection.Upsert(entity2);
					return;
				}
				catch (Exception exception)
				{
					_logger.LogError(exception, "Upsert failed for object with key " + text + " on collection " + collectionName);
				}
				try
				{
					collection.Delete(text);
				}
				catch (Exception exception2)
				{
					_logger.LogError(exception2, "Delete failed after failed upsert with key " + text);
				}
				try
				{
					collection.Upsert(entity2);
				}
				catch (Exception exception3)
				{
					_logger.LogError(exception3, "Upsert retry failed for object with key " + text);
				}
			}
		}
		catch (Exception exception4)
		{
			_logger.LogError(exception4, "Failed to set " + typeof(T).FullName + " into cache");
		}
	}

	private static Guid GetOrGenerateKey(string keyPath)
	{
		byte[] array = new byte[16]
		{
			239, 52, 217, 5, 65, 138, 125, 231, 194, 231,
			32, 138, 219, 8, 10, 148
		};
		if (File.Exists(keyPath))
		{
			try
			{
				return new Guid(ProtectedData.Unprotect(File.ReadAllBytes(keyPath), array, (DataProtectionScope)0));
			}
			catch (CryptographicException)
			{
				File.Delete(keyPath);
			}
		}
		Guid result = Guid.NewGuid();
		byte[] bytes = ProtectedData.Protect(result.ToByteArray(), array, (DataProtectionScope)0);
		File.WriteAllBytes(keyPath, bytes);
		return result;
	}

	private static string GetCollectionName<T>()
	{
		return s_collectionNameRegex.Replace(typeof(T).FullName, "_");
	}
}
