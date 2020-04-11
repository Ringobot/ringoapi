﻿using Ringo.Api.Data;
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

        public async Task SetRefreshToken(string userId, BearerAccessRefreshToken tokens)
        {
            var now = DateTimeOffset.UtcNow;
            var user = await _userData.Get(userId, userId);
            user.Tokens = tokens;
            user.Authorized = true;
            user.AccessTokenExpiresBefore = tokens.Expires ?? now.AddMinutes(tokens.ExpiresIn);
           
            await _userData.Replace(user, user.ETag);
        }
    }
}
