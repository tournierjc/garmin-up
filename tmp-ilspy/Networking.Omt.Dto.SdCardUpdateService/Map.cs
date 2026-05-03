using Networking.Omt.Dto.Common;

namespace Networking.Omt.Dto.SdCardUpdateService;

public record Map(Release Release, string DisplayName, string MapType, FileToRemove[] FilesToRemove, Identifier Identifier, File[] Files, bool IsReinstall, bool CanUninstall, PurchasableUpdate PurchasableUpdate, string[] EulaUrls, DownloadHosts DownloadHosts);
