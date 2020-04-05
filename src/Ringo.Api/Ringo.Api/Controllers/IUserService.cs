using Ringo.Api.Services;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Ringo.Api.Controllers
{
    public interface IUserService
    {
        Task<User> GetUser(ClaimsPrincipal user);
    }
}