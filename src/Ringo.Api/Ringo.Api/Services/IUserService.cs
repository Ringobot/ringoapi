using Ringo.Api.Models;
using SpotifyApi.NetCore.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Ringo.Api.Services
{
    public interface IUserService
    {
        Task<User> GetUser(string userId);
        Task<User> CreateUser(string userId);
        Task SetRefreshToken(string userId, BearerAccessRefreshToken tokens);
    }
}