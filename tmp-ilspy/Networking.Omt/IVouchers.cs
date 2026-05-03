using System.Threading.Tasks;
using Networking.Omt.Dto.Vouchers;

namespace Networking.Omt;

public interface IVouchers
{
	Task<VoucherCodeDetailsResponse> GetVoucherCodeDetailsAsync(VoucherCodeDetailsRequest request);

	Task RedeemVoucherCodeAsync(RedeemVoucherCodeRequest request);
}
