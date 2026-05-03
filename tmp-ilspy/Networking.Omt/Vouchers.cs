using System.Threading.Tasks;
using Networking.Omt.Dto.Vouchers;
using RestSharp;

namespace Networking.Omt;

internal sealed class Vouchers : IVouchers
{
	private readonly OmtRestClient _restClient;

	public Vouchers(OmtRestClient restClient)
	{
		_restClient = restClient;
	}

	public async Task<VoucherCodeDetailsResponse> GetVoucherCodeDetailsAsync(VoucherCodeDetailsRequest request)
	{
		RestRequest request2 = new RestRequest("api/vouchers/" + request.VoucherCode);
		return (await _restClient.ExecuteAsync<VoucherCodeDetailsResponse>(request2)).Data;
	}

	public async Task RedeemVoucherCodeAsync(RedeemVoucherCodeRequest request)
	{
		RestRequest request2 = new RestRequest($"/api/vouchers/{request.VoucherCode}/unit/{request.UnitId}/product/{request.ProductPartNumber}/customer/{request.CustomerId}", Method.Post);
		request2.AddJsonBody(request);
		await _restClient.ExecuteAsync(request2);
	}
}
