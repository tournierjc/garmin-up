namespace Networking.Omt.Dto.DlcService;

public record DownloadableContent(string[] EulaUrls, string PartNumber, DlcDownload[] DlcDownloads);
