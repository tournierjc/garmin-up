using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Autofac;
using Autofac.Core;
using Autofac.Core.Resolving.Pipeline;
using Microsoft.Extensions.Logging;
using Networking.Connect;
using Networking.Connect.Dto.UserProfileService;
using Networking.ConnectIqStore;
using Networking.DownloadManager;
using Networking.GcsApi;
using Networking.Geolocation;
using Networking.Ghs;
using Networking.ITServices;
using Networking.InternalOmt;
using Networking.OAuth;
using Networking.Omt;
using Networking.Omt.Analytics;
using Networking.Static;
using Networking.SyncServices;
using Newtonsoft.Json;
using RestSharp;
using Utilities;

namespace Networking;

public class NetworkingModule : Autofac.Module
{
	private readonly Guid _installationId;

	private readonly Guid _sessionId;

	private readonly string _clientId;

	private readonly Version _appVersion;

	private readonly string _ssoAppId;

	private readonly string _diAuthClientId;

	private readonly GarminEnvironment _environment;

	static NetworkingModule()
	{
		ServicePointManager.DefaultConnectionLimit = 3;
	}

	public static async Task<NetworkingModule> GetInstance(Guid installationId, Guid sessionId, string clientId, Version appVersion, string ssoAppId, string diAuthClientId, ILoggerFactory loggerFactory, GarminEnvironment? environment = null, string? countryCode = null)
	{
		if (environment.HasValue)
		{
			return new NetworkingModule(installationId, sessionId, clientId, appVersion, ssoAppId, diAuthClientId, environment.Value);
		}
		ILogger<NetworkingModule> logger = loggerFactory.CreateLogger<NetworkingModule>();
		OverrideSettings overrideSettings = null;
		try
		{
			string path = Path.Combine(PathUtils.GetAppDataPath(), "overrides.json");
			if (File.Exists(path))
			{
				overrideSettings = JsonConvert.DeserializeObject<OverrideSettings>(File.ReadAllText(path));
			}
		}
		catch (Exception exception)
		{
			logger.LogError(exception, "Unable to deserialize override settings. Using defaults.");
		}
		if ((object)overrideSettings != null && overrideSettings.Environment.HasValue)
		{
			return new NetworkingModule(installationId, sessionId, clientId, appVersion, ssoAppId, diAuthClientId, overrideSettings.Environment.Value);
		}
		NetworkingSettings networkSettings = NetworkingSettings.Get();
		if (countryCode != null)
		{
			networkSettings.CountryCode = countryCode;
			networkSettings.Save();
		}
		else if (networkSettings.CountryCode == null)
		{
			GarminEnvironment[] array = new GarminEnvironment[2]
			{
				GarminEnvironment.Production,
				GarminEnvironment.China
			};
			foreach (GarminEnvironment garminEnvironment in array)
			{
				try
				{
					GeolocationRequests geolocationRequests = new GeolocationRequests(new GeolocationRestClient(loggerFactory.CreateLogger<GeolocationRestClient>(), garminEnvironment));
					NetworkingSettings networkingSettings = networkSettings;
					networkingSettings.CountryCode = await ((IGeolocationRequests)geolocationRequests).GetGeolocationAsync();
					networkSettings.Save();
				}
				catch (Exception exception2)
				{
					logger.LogWarning(exception2, "NetworkingModule.GetInstance GetGeolocationAsync Failed");
					continue;
				}
				break;
			}
		}
		environment = GetEnvironmentForCountryCode(networkSettings.CountryCode ?? RegionInfo.CurrentRegion.TwoLetterISORegionName);
		return new NetworkingModule(installationId, sessionId, clientId, appVersion, ssoAppId, diAuthClientId, environment.Value);
	}

	private NetworkingModule(Guid installationId, Guid sessionId, string clientId, Version appVersion, string ssoAppId, string diAuthClientId, GarminEnvironment environment)
	{
		_installationId = installationId;
		_sessionId = sessionId;
		_clientId = clientId;
		_appVersion = appVersion;
		_ssoAppId = ssoAppId;
		_diAuthClientId = diAuthClientId;
		_environment = environment;
		LegacyOverrides.GarminEnvironment = environment;
	}

	protected override void Load(ContainerBuilder builder)
	{
		builder.Register((IComponentContext _) => _environment);
		builder.RegisterInstance(new InstallationGuid(_installationId));
		builder.RegisterInstance(new SessionGuid(_sessionId));
		builder.RegisterInstance(new ClientId(_clientId));
		builder.RegisterInstance(new AppVersion(_appVersion));
		builder.RegisterInstance(new SsoAppId(_ssoAppId));
		builder.RegisterInstance(new DIAuthClientId(_diAuthClientId));
		builder.RegisterType<Networking.DownloadManager.DownloadManager>().As<IDownloadManager>().WithParameter(TypedParameter.From((IRestClient)new RestClient(delegate(RestClientOptions i)
		{
			i.Timeout = TimeSpan.FromMinutes(5.0);
		})));
		builder.RegisterType<DiskCache>().As<ICache>().SingleInstance();
		builder.RegisterType<AuthManager>().AsImplementedInterfaces().SingleInstance();
		(from i in builder.RegisterAssemblyTypes(GetType().Assembly)
			where i.IsSubclassOf(typeof(NetworkingRestClient))
			select i).OnPreparing((Action<PreparingEventArgs>)ForwardFactoryParameters);
		builder.RegisterType<WebtoolsRestClient>();
		builder.RegisterType<NetworkingRestClient>();
		Type[] types = GetType().Assembly.GetTypes();
		foreach (Type type in types)
		{
			foreach (NetworkingServiceAttribute customAttribute in type.GetCustomAttributes<NetworkingServiceAttribute>())
			{
				builder.RegisterType(type).As(customAttribute.InterfaceType);
			}
		}
		builder.RegisterType<DeviceService>().As<IDeviceService>();
		builder.RegisterType<UserPreferenceService>().As<IUserPreferenceService>();
		builder.RegisterType<UserProfileService>().As<IUserProfileService>();
		builder.RegisterType<Ephemeris>().As<IEphemeris>();
		builder.RegisterType<GeolocationRequests>().As<IGeolocationRequests>();
		builder.RegisterType<AccountProcessService>().As<IAccountProcessService>();
		builder.RegisterType<Analytics>().As<IAnalytics>();
		builder.RegisterType<AppConfig>().As<IAppConfig>();
		builder.RegisterType<ApplicationService>().As<IApplicationService>();
		builder.RegisterType<DeviceSoftware>().As<IDeviceSoftware>();
		builder.RegisterType<Gemini>().As<IGemini>();
		builder.RegisterType<MapUpdateService>().As<IMapUpdateService>();
		builder.RegisterType<MarineSubscriptionService>().As<IMarineSubscriptionService>();
		builder.RegisterType<ShopService>().As<IShopService>();
		builder.RegisterType<SoftwareUpdateService>().As<ISoftwareUpdateService>();
		builder.RegisterType<UnitService>().As<IUnitService>();
		builder.RegisterType<Units>().As<IUnits>();
		builder.RegisterType<ProductGroups>().As<IProductGroups>();
		builder.RegisterType<Releases>().As<IReleases>();
		builder.RegisterType<Contents>().As<IContents>();
		builder.RegisterType<Regions>().As<IRegions>();
		builder.RegisterType<UniversalMaps>().As<IUniversalMaps>();
		builder.RegisterType<Vouchers>().As<IVouchers>();
		builder.RegisterType<StaticRequests>().As<IStaticRequests>();
		builder.RegisterType<SdCardUpdateService>().As<ISdCardUpdateService>();
		builder.RegisterType<NetworkSpeedTest>().As<INetworkSpeedTest>();
		builder.RegisterType<WebtoolsService>().As<IWebtoolsService>();
		builder.RegisterType<AppSettingsService>().As<IAppSettingsService>();
		builder.RegisterType<SsoRequests>().As<ISsoRequests>();
		builder.RegisterType<SyncUploadService>().As<ISyncUploadService>();
		builder.RegisterType<AccountService>().As<IAccountService>();
		builder.RegisterType<MarineAccountService>().As<IMarineAccountService>();
		builder.RegisterType<MarineChartService>().As<IMarineChartService>();
		builder.RegisterType<XmlHacks>().As<IXmlHacks>();
		builder.RegisterType<DeviceBackupService>().As<IDeviceBackupService>();
		builder.RegisterType<CustomerBusinessService>().As<ICustomerBusinessService>();
		builder.RegisterType<ConsentService>().As<IConsentService>();
		builder.RegisterType<ConsentTextServices>().As<IConsentTextServices>();
		builder.RegisterType<UploadConsent>().As<IUploadConsent>();
		builder.RegisterType<AppServices>().As<IAppServices>();
		builder.Register(delegate(IComponentContext i)
		{
			CustomerGuid customerGuid = ResolutionExtensions.ResolveOptional<CustomerGuid>(i);
			if ((object)customerGuid != null)
			{
				return new Optional<CustomerGuid>(customerGuid);
			}
			UnitId unitId = ResolutionExtensions.ResolveOptional<UnitId>(i);
			return ((object)unitId != null) ? new Optional<CustomerGuid>(i.Resolve<IAuthDataProvider>().GetCustomerGuid(unitId)) : new Optional<CustomerGuid>(null);
		});
		builder.Register(delegate(IComponentContext i)
		{
			ConnectAccount connectAccount = ResolutionExtensions.ResolveOptional<ConnectAccount>(i);
			if (connectAccount != null)
			{
				return new Optional<ConnectAccount>(connectAccount);
			}
			UnitId unitId = ResolutionExtensions.ResolveOptional<UnitId>(i);
			return ((object)unitId != null) ? new Optional<ConnectAccount>(i.Resolve<IAuthDataProvider>().GetConnectAccount(unitId)) : new Optional<ConnectAccount>(null);
		});
		builder.Register(delegate(IComponentContext i)
		{
			UnitId unitId = ResolutionExtensions.ResolveOptional<UnitId>(i);
			return ((object)unitId != null) ? new Optional<UnitId>(unitId) : new Optional<UnitId>(null);
		});
	}

	private static GarminEnvironment GetEnvironmentForCountryCode(string countryCode)
	{
		if (!string.Equals(countryCode, "CN", StringComparison.InvariantCultureIgnoreCase))
		{
			return GarminEnvironment.Production;
		}
		return GarminEnvironment.China;
	}

	public static void ForwardFactoryParameters(PreparingEventArgs e)
	{
		ResolveRequestContext resolveRequestContext = (ResolveRequestContext)e.Context;
		if (resolveRequestContext.Operation.InitiatingRequest.HasValue)
		{
			e.Parameters = resolveRequestContext.Operation.InitiatingRequest.Value.Parameters;
		}
	}
}
