namespace Networking.Omt.Dto.Vouchers;

public class RedeemVoucherCodeRequest
{
	public string VoucherCode { get; }

	public uint UnitId { get; }

	public string ProductPartNumber { get; }

	public string CustomerId { get; }

	public RedeemVoucherCodeRequest(string voucherCode, uint unitId, string productPartNumber, string customerId)
	{
		VoucherCode = voucherCode;
		UnitId = unitId;
		ProductPartNumber = productPartNumber;
		CustomerId = customerId;
	}
}
