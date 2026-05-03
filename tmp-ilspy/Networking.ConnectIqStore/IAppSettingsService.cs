using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RestSharp;

namespace Networking.ConnectIqStore;

public interface IAppSettingsService
{
	Task<RestResponse> GetSavedAppSettings(string locale, Guid appId, uint version, string firmwarePartNumber, string javaScriptResponse);

	Task<RestResponse> GetPersistedAppSettingsHtml(string locale, Guid appId, uint version, string firmwarePartNumber, Dictionary<object, object> settingsDict);
}
