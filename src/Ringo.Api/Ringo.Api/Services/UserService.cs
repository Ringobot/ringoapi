using Ringo.Api.Data;
using Ringo.Api.Models;
using System;
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

        public async Task<User> SetPlayer(string userId, NowPlaying np)
        {
            var user = await GetUser(userId);

            user.Player = new Player
            {
                Artist = np.Track?.Artists[0]?.Name,
                Track = np.Track?.Name,
                IsPlaying = np.IsPlaying
            };

            if (np.Context != null)
            {
                user.Player.Context = new Context { Type = np.Context?.Type, Uri = np.Context?.Uri };
            }

            if (np.Offset != null)
            {
                user.Player.Epoch = np.Offset.Epoch;
                user.Player.PositionMsAtEpoch = Convert.ToInt32(np.Offset.PositionAtEpoch.TotalMilliseconds);
            }

            return await _userData.Replace(user, user.ETag);
        }
    }
}
