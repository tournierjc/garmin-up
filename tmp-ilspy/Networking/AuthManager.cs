using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Networking.Auth;
using Networking.Connect;
using Networking.Connect.Dto.UserProfileService;
using Networking.ITServices;
using Networking.OAuth;
using Networking.Omt;
using RestSharp;

namespace Networking;

internal sealed class AuthManager : IAuthManager, ITicketExchanger, IAuthDataProvider
{
	private record JwtTokenResponse(string Token);

	private record SsoTokenResponse(string LoginToken, string Username, string Service);

	private readonly ILogger<AuthManager> _logger;

	private readonly ICache _cache;

	private readonly string _diAuthClientId;

	private readonly string _ssoAppId;

	private readonly Func<ConnectRestClient> _connectRestClient;

	private readonly Lazy<ITRestClient> _itRestClient;

	private readonly Lazy<OmtRestClient> _omtRestClient;

	private readonly Lazy<SsoRestClient> _ssoRestClient;

	public AuthManager(ILogger<AuthManager> logger, ICache cache, SsoAppId ssoAppId, DIAuthClientId diAuthClientId, Func<ConnectRestClient> connectRestClient, Lazy<ITRestClient> itRestClient, Lazy<SsoRestClient> ssoRestClient, Lazy<OmtRestClient> omtRestClient)
	{
		_logger = logger;
		_cache = cache;
		_ssoAppId = ssoAppId.Id;
		_diAuthClientId = diAuthClientId.Id;
		_connectRestClient = connectRestClient;
		_itRestClient = itRestClient;
		_ssoRestClient = ssoRestClient;
		_omtRestClient = omtRestClient;
	}

	async Task<ConnectAuth> ITicketExchanger.GetConnectAuthAsync(ServiceTicket ticket)
	{
		ConnectRestClient connectRestClient = _connectRestClient();
		RestRequest request = new RestRequest("oauth-service/oauth/preauthorized")
		{
			Authenticator = ConnectTokenProvider.GetAuthenticatorForRequestToken()
		};
		request.AddParameter("ticket", ticket.Ticket);
		request.AddParameter("login-url", ticket.Url);
		NameValueCollection nameValueCollection = HttpUtility.ParseQueryString((await connectRestClient.ExecuteAsync(request)).Content);
		return new ConnectAuth
		{
			Token = nameValueCollection["oauth_token"],
			TokenSecret = nameValueCollection["oauth_token_secret"]
		};
	}

	async Task<DIToken> ITicketExchanger.GetDIAuthAsync(ServiceTicket ticket)
	{
		ConnectRestClient connectRestClient = _connectRestClient();
		RestRequest request = new RestRequest("/di-oauth2-service/oauth/token", Method.Post);
		request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
		request.AddParameter("client_id", _diAuthClientId);
		request.AddParameter("grant_type", "https://connectapi.garmin.com/di-oauth2-service/oauth/grant/service_ticket");
		request.AddParameter("service_ticket", ticket.Ticket);
		request.AddParameter("service_url", ticket.Url);
		DITokenResponse data = (await connectRestClient.ExecuteAsync<DITokenResponse>(request)).Data;
		return new DIToken
		{
			AccessToken = data.AccessToken,
			Expiration = data.Expiration,
			Jti = data.Jti,
			MfaToken = data.MfaToken,
			MfaExpirationTimestamp = data.MfaExpirationTimestamp,
			RefreshToken = data.RefreshToken,
			RefreshTokenExpiration = data.RefreshTokenExpiration,
			Scope = data.Scope,
			TokenType = data.TokenType
		};
	}

	async Task<ITAuth> ITicketExchanger.GetITAuthAsync(ServiceTicket ticket)
	{
		RestRequest request = new RestRequest("/api/oauth/token", Method.Post);
		request.AddParameter("client_id", _ssoAppId);
		request.AddParameter("grant_type", "service_ticket");
		request.AddParameter("service_ticket", ticket.Ticket);
		request.AddParameter("service_url", ticket.Url);
		RestResponse<ServicesTokenResponse> restResponse = await _itRestClient.Value.ExecuteAsync<ServicesTokenResponse>(request);
		return new ITAuth
		{
			AccessToken = restResponse.Data.AccessToken,
			CustomerId = restResponse.Data.CustomerId,
			Expiration = restResponse.Data.Expiration,
			RefreshToken = restResponse.Data.RefreshToken,
			RefreshTokenExpiration = restResponse.Data.RefreshTokenExpiration,
			Scope = restResponse.Data.Scope,
			TokenType = restResponse.Data.TokenType
		};
	}

	void IAuthManager.AddAuth(UnitId unitId, DIToken diToken)
	{
		DeviceAuth deviceAuth = _cache.Get(unitId);
		deviceAuth.DIToken = diToken;
		_cache.Set(deviceAuth);
	}

	void IAuthManager.AddAuth(UnitId unitId, ITAuth auth)
	{
		DeviceAuth deviceAuth = _cache.Get(unitId);
		deviceAuth.ITAuth = auth;
		_cache.Set(deviceAuth);
	}

	void IAuthManager.AddExistingAuth(ITAuth auth)
	{
		AccountAuth accountAuth = _cache.Get(new CustomerGuid(auth.CustomerId));
		accountAuth.ITAuth = auth;
		_cache.Set(accountAuth);
	}

	void IAuthManager.AddNewAuth(UnitId unitId, ConnectAuth auth, ConnectAccount? acct)
	{
		_cache.DeleteAuth(unitId);
		DeviceAuth deviceAuth = _cache.Get(unitId);
		deviceAuth.ConnectAuth = auth;
		deviceAuth.ConnectAccount = acct;
		_cache.Set(deviceAuth);
	}

	void IAuthManager.AddNewAuth(ITAuth auth)
	{
		CustomerGuid customerGuid = new CustomerGuid(auth.CustomerId);
		_cache.DeleteAuth(customerGuid);
		AccountAuth accountAuth = _cache.Get(customerGuid);
		accountAuth.ITAuth = auth;
		_cache.Set(accountAuth);
	}

	void IAuthManager.DeleteAuth(UnitId unitId)
	{
		_cache.DeleteAuth(unitId);
	}

	void IAuthManager.DeleteAuth(CustomerGuid customerGuid)
	{
		_cache.DeleteAuth(customerGuid);
	}

	async Task<bool> IAuthManager.ValidateITAuth(CustomerGuid customerGuid)
	{
		try
		{
			return await ((IAuthDataProvider)this).GetITAuthAsync(customerGuid) != null;
		}
		catch
		{
			return false;
		}
	}

	bool IAuthDataProvider.HasConnectAuth(UnitId unitId)
	{
		return _cache.Get(unitId).ConnectAuth != null;
	}

	bool IAuthDataProvider.HasITAuth(CustomerGuid customerGuid)
	{
		return _cache.Get(customerGuid).ITAuth != null;
	}

	ConnectAuth? IAuthDataProvider.GetConnectAuth(UnitId unitId)
	{
		return _cache.Get(unitId).ConnectAuth;
	}

	ConnectAccount? IAuthDataProvider.GetConnectAccount(UnitId unitId)
	{
		return _cache.Get(unitId).ConnectAccount;
	}

	async Task<DIToken?> IAuthDataProvider.GetDIAuthAsync(UnitId unitId)
	{
		DeviceAuth deviceAuth = _cache.Get(unitId);
		if (deviceAuth.DIToken == null)
		{
			return null;
		}
		if (deviceAuth.DIToken.Expiration > DateTime.Now)
		{
			return deviceAuth.DIToken;
		}
		return await GetDIAuthAsync(unitId, deviceAuth.DIToken);
	}

	async Task<ITAuth?> IAuthDataProvider.GetITAuthAsync(UnitId unitId)
	{
		DeviceAuth auth = _cache.Get(unitId);
		if (auth.ITAuth?.Expiration > DateTime.Now)
		{
			return auth.ITAuth;
		}
		if (auth.ITAuth != null)
		{
			if (auth.ITAuth.Expiration > DateTime.Now)
			{
				return auth.ITAuth;
			}
			try
			{
				return await GetITAuthAsync(unitId, null, auth.ITAuth);
			}
			catch
			{
				_logger.LogWarning($"Failed to refresh IT Auth for device {unitId.Id}.");
			}
		}
		return await GetITAuthAsync(unitId, auth.ConnectAuth);
	}

	async Task<ITAuth?> IAuthDataProvider.GetITAuthAsync(CustomerGuid customerGuid)
	{
		AccountAuth accountAuth = _cache.Get(customerGuid);
		if (accountAuth.ITAuth == null)
		{
			return null;
		}
		if (accountAuth.ITAuth.Expiration > DateTime.Now)
		{
			return accountAuth.ITAuth;
		}
		return await GetITAuthAsync(null, null, accountAuth.ITAuth);
	}

	async Task<JsonWebToken?> IAuthDataProvider.GetOmtAuthAsync(CustomerGuid customerGuid)
	{
		AccountAuth accountAuth = _cache.Get(customerGuid);
		if (accountAuth.OmtAuth != null)
		{
			JsonWebToken jsonWebToken = new JsonWebToken(accountAuth.OmtAuth.Token);
			if (jsonWebToken.ValidTo > DateTime.UtcNow)
			{
				return jsonWebToken;
			}
		}
		ITAuth itAuth = await ((IAuthDataProvider)this).GetITAuthAsync(customerGuid);
		if (itAuth == null)
		{
			return null;
		}
		RestRequest request = new RestRequest($"/api/auth/tokens/{itAuth.CustomerId}", Method.Post);
		request.AddHeader("Authorization", "Bearer " + itAuth.AccessToken);
		RestResponse<JwtTokenResponse> restResponse = await _omtRestClient.Value.ExecuteAsync<JwtTokenResponse>(request);
		AccountAuth accountAuth2 = _cache.Get(new CustomerGuid(itAuth.CustomerId));
		accountAuth2.OmtAuth = new OmtJwtAuth
		{
			Token = restResponse.Data.Token
		};
		_cache.Set(accountAuth2);
		return new JsonWebToken(accountAuth2.OmtAuth.Token);
	}

	void IAuthDataProvider.ExpireITAuth(CustomerGuid customerGuid)
	{
		AccountAuth accountAuth = _cache.Get(customerGuid);
		if (accountAuth.ITAuth != null)
		{
			accountAuth.ITAuth = accountAuth.ITAuth with
			{
				Expiration = DateTime.MinValue
			};
			_cache.Set(accountAuth);
		}
	}

	CustomerGuid? IAuthDataProvider.GetCustomerGuid(UnitId unitId)
	{
		DeviceAuth deviceAuth = _cache.Get(unitId);
		if (Guid.TryParse(deviceAuth.ConnectAccount?.GarminGuid, out var result))
		{
			return new CustomerGuid(result);
		}
		if (deviceAuth.ITAuth != null)
		{
			return new CustomerGuid(deviceAuth.ITAuth.CustomerId);
		}
		return null;
	}

	private async Task<DIToken?> GetDIAuthAsync(UnitId unitId, DIToken expiredDIToken)
	{
		ConnectRestClient connectRestClient = _connectRestClient();
		RestRequest request = new RestRequest("/di-oauth2-service/oauth/token", Method.Post);
		request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
		request.AddParameter("client_id", _diAuthClientId);
		request.AddParameter("grant_type", "refresh_token");
		request.AddParameter("refresh_token", expiredDIToken.RefreshToken);
		RestResponse<DITokenResponse> restResponse = await connectRestClient.ExecuteAsync<DITokenResponse>(request);
		if (restResponse.Data == null)
		{
			return null;
		}
		DIToken dIToken = new DIToken
		{
			AccessToken = restResponse.Data.AccessToken,
			Expiration = restResponse.Data.Expiration,
			Jti = restResponse.Data.Jti,
			MfaToken = restResponse.Data.MfaToken,
			MfaExpirationTimestamp = restResponse.Data.MfaExpirationTimestamp,
			RefreshToken = restResponse.Data.RefreshToken,
			RefreshTokenExpiration = restResponse.Data.RefreshTokenExpiration,
			Scope = restResponse.Data.Scope,
			TokenType = restResponse.Data.TokenType
		};
		DeviceAuth deviceAuth = _cache.Get(unitId);
		deviceAuth.DIToken = dIToken;
		_cache.Set(deviceAuth);
		return dIToken;
	}

	private async Task<ITAuth?> GetITAuthAsync(UnitId? unitId = null, ConnectAuth? connectAuth = null, ITAuth? expiredITAuth = null)
	{
		RestRequest request = new RestRequest("/api/oauth/token", Method.Post);
		request.AddParameter("client_id", _ssoAppId);
		if (unitId != null && connectAuth != null)
		{
			request.AddParameter("grant_type", "connect_exchange");
			request.AddParameter("connect_access_token", connectAuth.Token);
		}
		else
		{
			if (!(expiredITAuth != null))
			{
				return null;
			}
			request.AddParameter("grant_type", "refresh_token");
			request.AddParameter("refresh_token", expiredITAuth.RefreshToken);
		}
		RestResponse<ServicesTokenResponse> restResponse = await _itRestClient.Value.ExecuteAsync<ServicesTokenResponse>(request);
		if (restResponse.Data == null)
		{
			return null;
		}
		ITAuth iTAuth = new ITAuth
		{
			AccessToken = restResponse.Data.AccessToken,
			CustomerId = restResponse.Data.CustomerId,
			Expiration = restResponse.Data.Expiration,
			RefreshToken = restResponse.Data.RefreshToken,
			RefreshTokenExpiration = restResponse.Data.RefreshTokenExpiration,
			Scope = restResponse.Data.Scope,
			TokenType = restResponse.Data.TokenType
		};
		if (unitId != null)
		{
			DeviceAuth deviceAuth = _cache.Get(unitId);
			deviceAuth.ITAuth = iTAuth;
			_cache.Set(deviceAuth);
		}
		else
		{
			AccountAuth accountAuth = _cache.Get(new CustomerGuid(restResponse.Data.CustomerId));
			accountAuth.ITAuth = iTAuth;
			_cache.Set(accountAuth);
		}
		return iTAuth;
	}
}
