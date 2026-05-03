using System.Collections.Generic;
using Networking.Omt.Dto.Common;

namespace Networking.Omt.Dto.MapUpdateService;

public record DeliverableContent(List<DeliverableContent> AdditionalContent, FileToRemove? ContentToReplace, string? ContentType, string? DisplayName, List<DeliverableContent> ExtraContents, List<FileToRemove> ExtraContentsToReplace, byte[]? Gma, int Id, bool IsPackage, bool IsRecommended, bool IsReinstallOnly, string? Locale, string? PartNumber, string? PartNumberToReplace, List<DeliverableContent> SmallerContentOptions, string? UnlockCode, List<UrlDto> Urls);
