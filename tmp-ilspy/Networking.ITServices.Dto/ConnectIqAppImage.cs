using System;

namespace Networking.ITServices.Dto;

public class ConnectIqAppImage
{
	public required Guid AppId { get; init; }

	public required Uri IconUrl { get; init; }
}
