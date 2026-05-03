using System;

namespace Networking.ITServices.Dto.ConnectIq;

public record IqAppRequest(Guid AppId, uint InternalVersionNumber);
