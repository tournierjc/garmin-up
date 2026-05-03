using System.Collections.Generic;

namespace Networking.InternalOmt.Dto;

public record Content(List<ContentType> ContentTypes, string? DefaultTranslation, string? DestinationFileName, string? DisplayName, string? DownloadUrl, Fprs Fprs, bool IsActive, string? LargerContentPartNumber, List<string> LargerContentPartNumbers, string? Locale, string? ManifestUrl, string Md5, List<string> NewerContentPartNumbers, string? PartNumber, List<PreviewImage> PreviewImages, string? PreviewImageUrl, List<string> PreviousContentPartNumbers, long? SizeInBytes, List<string> SmallerContents, string? AdditionsUrl, string RegionsUrl, string TranslationsUrl);
