using Networking.Omt.Dto.Common;

namespace Networking.Omt.Dto.SdCardUpdateService;

public record PurchasableUpdate(string PartNumber, string CardPartNumber, string DisplayName, Release Release, long SizeInBytes);
