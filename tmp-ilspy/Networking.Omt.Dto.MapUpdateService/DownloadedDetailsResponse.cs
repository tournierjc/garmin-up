using System.Collections.Generic;
using Networking.Omt.Dto.Common;

namespace Networking.Omt.Dto.MapUpdateService;

public record DownloadedDetailsResponse(List<FileToRemove> FilesToRemove, List<DeliverableContent> AvailableContents, string? ComputerInstallUrl, DownloadHosts? DownloadHosts);
