using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Networking.Omt.Dto.MarineAccountService;
using RestSharp;

namespace Networking.Omt;

internal sealed class MarineAccountService : IMarineAccountService
{
	private record GetRegisteredDevicesForCustomerRequest(Guid CustomerGuid, string Locale);

	private record GetRegisteredDevicesForCustomerResponse
	{
		public RegisteredDeviceCategory[] DeviceCategories { get; } = DeviceCategories ?? new RegisteredDeviceCategory[0];

		public GetRegisteredDevicesForCustomerResponse(RegisteredDeviceCategory[]? DeviceCategories)
		{
		}

		[CompilerGenerated]
		public void Deconstruct(out RegisteredDeviceCategory[]? DeviceCategories)
		{
			DeviceCategories = this.DeviceCategories;
		}
	}

	private record RegisterCardsToCustomerRequest(Guid CustomerGuid, string[] Gmas);

	private record RegisterCardsToCustomerResponse
	{
		public CardRegistration[] CardRegistrations { get; } = CardRegistrations ?? new CardRegistration[0];

		public RegisterCardsToCustomerResponse(CardRegistration[]? CardRegistrations)
		{
		}

		[CompilerGenerated]
		public void Deconstruct(out CardRegistration[]? CardRegistrations)
		{
			CardRegistrations = this.CardRegistrations;
		}
	}

	private record RegisterDevicesToCustomerRequest(Guid CustomerGuid, long[] UnitIds);

	private record RegisterDevicesToCustomerResponse
	{
		public DeviceRegistration[] DeviceRegistrations { get; } = DeviceRegistrations ?? new DeviceRegistration[0];

		public RegisterDevicesToCustomerResponse(DeviceRegistration[]? DeviceRegistrations)
		{
		}

		[CompilerGenerated]
		public void Deconstruct(out DeviceRegistration[]? DeviceRegistrations)
		{
			DeviceRegistrations = this.DeviceRegistrations;
		}
	}

	private readonly OmtRestClient _restClient;

	private readonly CustomerGuid _customerGuid;

	public MarineAccountService(OmtRestClient restClient, CustomerGuid customerGuid)
	{
		_restClient = restClient;
		_customerGuid = customerGuid;
	}

	public async Task<RegisteredDeviceCategory[]> GetRegisteredDevicesForCustomerAsync(string locale)
	{
		GetRegisteredDevicesForCustomerRequest obj = new GetRegisteredDevicesForCustomerRequest(_customerGuid.Guid, locale);
		RestRequest request = new RestRequest("MapUpdateService/Marine/RegisteredDevicesForCustomer", Method.Post);
		request.AddJsonBody(obj);
		return (await _restClient.ExecuteAsync<GetRegisteredDevicesForCustomerResponse>(request)).Data.DeviceCategories;
	}

	public async Task<CardRegistration[]> RegisterCardsToCustomerAsync(string[] gmas)
	{
		RegisterCardsToCustomerRequest obj = new RegisterCardsToCustomerRequest(_customerGuid.Guid, gmas);
		RestRequest request = new RestRequest("MapUpdateService/Marine/RegisterCardToCustomer", Method.Post);
		request.AddJsonBody(obj);
		return (await _restClient.ExecuteAsync<RegisterCardsToCustomerResponse>(request)).Data.CardRegistrations;
	}

	public async Task<DeviceRegistration[]> RegisterDevicesToCustomerAsync(long[] unitIds)
	{
		RegisterDevicesToCustomerRequest obj = new RegisterDevicesToCustomerRequest(_customerGuid.Guid, unitIds);
		RestRequest request = new RestRequest("MapUpdateService/Marine/RegisterDevicesToCustomer", Method.Post);
		request.AddJsonBody(obj);
		return (await _restClient.ExecuteAsync<RegisterDevicesToCustomerResponse>(request)).Data.DeviceRegistrations;
	}
}
