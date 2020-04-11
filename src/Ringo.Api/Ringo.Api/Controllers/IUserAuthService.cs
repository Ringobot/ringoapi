using Microsoft.AspNetCore.Http;
using Ringo.Api.Models;
using System.Threading.Tasks;

namespace Ringo.Api.Controllers
{
    public interface IUserAuthService
    {
        Task<User> Authorize(HttpRequest request);
    }
}