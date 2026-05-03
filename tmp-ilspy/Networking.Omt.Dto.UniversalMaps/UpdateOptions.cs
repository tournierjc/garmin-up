namespace Networking.Omt.Dto.UniversalMaps;

public record UpdateOptions(MapGroupingMode GroupingMode = MapGroupingMode.None, MapExtractionOption ExtractionMode = MapExtractionOption.None);
