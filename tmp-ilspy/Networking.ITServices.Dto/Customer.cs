using System;

namespace Networking.ITServices.Dto;

public record Customer
{
	public Guid id { get; init; }

	public Email? PrimaryEmailAddress { get; init; }

	public string? Locale { get; init; }

	public string? UserName { get; init; }
}
