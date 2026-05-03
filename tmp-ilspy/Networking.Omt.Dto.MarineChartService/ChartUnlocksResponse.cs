namespace Networking.Omt.Dto.MarineChartService;

public record ChartUnlocksResponse(ChartUnlock[] DeviceUnlocks, byte[] Gma);
