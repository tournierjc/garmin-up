using Networking.Omt.Dto.Common;

namespace Networking.Omt.Dto.SdCardUpdateService;

public record DeliverableOption(UrlType Type, string Url, string Md5, long SizeInBytes);
