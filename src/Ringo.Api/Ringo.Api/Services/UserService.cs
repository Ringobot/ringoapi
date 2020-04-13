using Ringo.Api.Data;
using Ringo.Api.Models;
using SpotifyApi.NetCore.Authorization;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Ringo.Api.Services
{
    public class UserService : IUserService
    {
        private readonly ICosmosData<User> _userData;

        public UserService(ICosmosData<User> userData)
        {
            _userData = userData;
        }

        public async Task<User> CreateUser(string userId)
        {
            return await _userData.Create(new User(userId));
        }

        public async Task<User> GetUser(string userId)
        {
            return await _userData.GetOrDefault(userId, userId);
        }
    }
}
