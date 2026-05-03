namespace Networking.Omt.Dto.SdCardUpdateService;

public record File(string PartNumber, string FileName, string DataType, DeliverableOption[] DeliverableOptions, string Locale);
