namespace Networking.Omt.Dto.Vouchers;

public class VoucherCodeDetailsRequest
{
	public string VoucherCode { get; }

	public VoucherCodeDetailsRequest(string voucherCode)
	{
		VoucherCode = voucherCode;
	}
}
