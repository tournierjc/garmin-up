using Networking.Omt.Dto.Common;

namespace Networking.Omt.Dto.DlcService;

public class ActivateHuntViewDlcResponse
{
	public UnlockInfo[]? UnlockInfos { get; set; }

	public EmbeddedUnlock[]? EmbeddedUnlockInfos { get; set; }
}
