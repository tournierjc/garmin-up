using Networking.Omt.Dto.Common;

namespace Networking.Omt.Dto.SdCardUpdateService;

public record SdCardContentsUnlock(string FileName, byte[] Data, ContentsUnlockCode[] Codes);
