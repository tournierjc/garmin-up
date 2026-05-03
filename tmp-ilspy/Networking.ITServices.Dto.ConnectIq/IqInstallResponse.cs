using System;

namespace Networking.ITServices.Dto.ConnectIq;

public record IqInstallResponse(Guid AppId, int Status);
