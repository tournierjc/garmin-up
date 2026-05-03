namespace Networking.Omt.Dto.DlcService;

public record DlcDownload(string PartNumber, string[] ContentTypes, string DownloadUrl, string DestinationFileName, string MD5, long SizeInBytes, DlcDownload[] AdditionalDownloads);
