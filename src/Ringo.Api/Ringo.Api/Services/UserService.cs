using Ringo.Api.Data;
using Ringo.Api.Models;
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

        public async Task<User> CreateUserIfNotExists(string userId)
        {
            var user = await GetUser(userId);
            if (user != null) return user;
            return await CreateUser(userId);
        }

        public async Task<User> GetUser(string userId)
        {
            return await _userData.GetOrDefault(userId, userId);
        }
    }
}
