using System.Collections.Generic;

namespace Networking.InternalOmt.Dto;

public record Region(List<Region> ChildRegions, Fprs Fprs, bool IsDefault, bool IsSubRegion, bool IsUnicode, bool IsUnitBased, string MapSourcePartNumber, string RegionPartNumber, Identifier ReleaseIdentifier, List<ContentType> SupportedContentTypes, string ContentsUrl);
