using Networking.Omt.Dto.Common;

namespace Networking.Omt.Dto.SdCardUpdateService;

public record ActivateSdCardUpdatesResponse(byte[] SignedSdCardBytes, SdCardContentsUnlock[] Unlocks, EmbeddedUnlock[] EmbeddedUnlocks);
