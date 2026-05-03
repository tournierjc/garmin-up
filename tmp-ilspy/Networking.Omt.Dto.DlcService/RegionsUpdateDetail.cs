namespace Networking.Omt.Dto.DlcService;

public record RegionsUpdateDetail(string ContentPartNumber, string[] DeviceNodes, bool IsReinstall, int RegionId, long SizeInBytes, string TokenizedDownloadUrl);
