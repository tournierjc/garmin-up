using System;
using System.Threading.Tasks;
using Networking.Omt.Dto.UploadConfigService;

namespace Networking.Omt;

public interface IUploadConfigService
{
	Task<UnitUploadSettings> GetUnitUploadSettingsAsync(string clientId, Guid clientGuid, bool enforceConsent);
}
