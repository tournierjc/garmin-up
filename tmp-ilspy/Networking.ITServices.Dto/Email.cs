namespace Networking.ITServices.Dto;

public record Email
{
	public string? EmailAddress { get; init; }

	public bool Primary { get; set; }
}
