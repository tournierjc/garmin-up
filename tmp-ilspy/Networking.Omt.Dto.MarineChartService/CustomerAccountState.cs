namespace Networking.Omt.Dto.MarineChartService;

public record CustomerAccountState(bool CanDownload, CustomerAccountStatus StatusCode = CustomerAccountStatus.Normal);
