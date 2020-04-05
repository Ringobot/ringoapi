using System.Threading.Tasks;

namespace Ringo.Api.Services
{
    public interface IUserStateService
    {
        string NewState(string userId);
        Task ValidateState(string state, string userId);
    }
}