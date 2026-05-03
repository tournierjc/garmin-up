namespace Networking.ITServices.Dto;

public class ConsentContent
{
	public ConsentCopy[]? ConsentCopyBlocks { get; set; }

	public ConsentLink[]? ConsentLinks { get; set; }

	public string? Description { get; set; }

	public string? GrantButton { get; set; }

	public string? Headline { get; set; }

	public required string Locale { get; set; }

	public string? RejectButton { get; set; }

	public string? RevokeCancelButton { get; set; }

	public string? RevokeConsentButton { get; set; }

	public ConsentCopy[]? RevokeCopyBlocks { get; set; }

	public string? RevokeHeadline { get; set; }

	public ConsentLink[]? RevokeLinks { get; set; }

	public required string Version { get; set; }
}
