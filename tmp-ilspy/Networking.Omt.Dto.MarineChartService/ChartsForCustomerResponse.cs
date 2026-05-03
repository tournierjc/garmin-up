using System;

namespace Networking.Omt.Dto.MarineChartService;

public record ChartsForCustomerResponse(Chart[] Charts, CustomerAccountState CustomerAccountStatus, DateTime? UtcTimeStamp);
