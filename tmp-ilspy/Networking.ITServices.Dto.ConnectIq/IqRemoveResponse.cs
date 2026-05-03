using System;

namespace Networking.ITServices.Dto.ConnectIq;

public record IqRemoveResponse(Guid AppId, int Status);
