﻿using Ringo.Api.Data;
using Ringo.Api.Models;
using SpotifyApi.NetCore.Authorization;
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
            return await _userData.Get(userId, userId);
        }

        public async Task SetRefreshToken(string userId, BearerAccessRefreshToken tokens)
        {
            var user = await GetUser(userId);
            user.Tokens = tokens;
            await _userData.Replace(user, user.ETag);
        }
    }
}
