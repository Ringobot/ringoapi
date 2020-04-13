//using Microsoft.Extensions.Logging;
//using Ringo.Api.Models;
//using Scale;
//using SpotifyApi.NetCore;
//using SpotifyApi.NetCore.Authorization;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.ServiceModel.Security;
//using System.Threading.Tasks;

//namespace Ringo.Api.Services
//{
//    public class SpotifyService : ISpotifyService
//    {
//        private readonly IPlayerApi _player;
//        private readonly ILogger<SpotifyService> _logger;
//        private readonly IUserAccountsService _userAccounts;
//        private readonly IUserService _userService;

//        public SpotifyService(
//            IPlayerApi playerApi,
//            ILogger<SpotifyService> logger,
//            IUserAccountsService userAccounts,
//            IUserService userService)
//        {
//            _player = playerApi;
//            _logger = logger;
//            _userAccounts = userAccounts;
//            _userService = userService;
//        }

//        public async Task PlayAlbumOffset(string uri, string trackId, Models.User user, long positionMs)
//        {
//            await RetryHelper.RetryAsync(
//                async () => _player.PlayAlbumOffset(
//                    uri,
//                    trackId,
//                    accessToken: await GetAccessToken(user),
//                    positionMs: positionMs),
//                    logger: _logger);
//        }

//        private async Task<string> GetAccessToken(Models.User user)
//        {
//            if (user.Authorized && !user.AccessTokenHasExpired) return user.Tokens.AccessToken;
//            if (!user.Authorized) throw new SecurityAccessDeniedException($"User \"{user.UserId}\" is not authorized");

//            // Refresh token
//            var newToken = await _userAccounts.RefreshUserAccessToken(user.Tokens.RefreshToken);
//            user.ResetAccessToken(newToken);
//            _userService.
//        }
//    }
//}
