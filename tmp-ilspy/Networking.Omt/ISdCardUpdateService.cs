using System;
using System.Threading.Tasks;
using Networking.Omt.Dto.SdCardUpdateService;

namespace Networking.Omt;

public interface ISdCardUpdateService
{
	Task<ActivateSdCardUpdatesResponse> ActivateAsync(byte[] signedSdCardBytes, Identifier identifier);

	Task RegisterSdCardAsync(Guid sdCardGuid);

	Task<DisplayInfoResponse> GetDisplayInfoAsync(byte[] signedSdCardBytes);

	Task<SdCardUpdateResponse> GetReinstallAsync(byte[] signedSdCardBytes, bool useCache = true);

	Task<SdCardUpdateResponse> GetUpdateAsync(byte[] signedSdCardBytes, bool useCache = true);
}
