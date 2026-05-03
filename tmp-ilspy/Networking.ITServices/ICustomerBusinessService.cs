using System.Threading.Tasks;
using Networking.ITServices.Dto;

namespace Networking.ITServices;

public interface ICustomerBusinessService
{
	Task<Customer> GetCustomerAsync();
}
