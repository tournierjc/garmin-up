using System;
using LiteDB;

namespace Networking.Auth;

internal sealed class AccountAuth
{
	[BsonId]
	public Guid CustomerId { get; set; }

	public ITAuth? ITAuth { get; set; }

	public OmtJwtAuth? OmtAuth { get; set; }
}
