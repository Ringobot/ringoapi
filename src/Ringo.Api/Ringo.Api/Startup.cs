using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ringo.Api.Controllers;
using Ringo.Api.Data;
using Ringo.Api.Models;
using Ringo.Api.Services;
using SpotifyApi.NetCore;
using SpotifyApi.NetCore.Authorization;
using System.Net.Http;

namespace Ringo.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers(options => options.Filters.Add<AuthSpotifyBearerFilter>());
            services.AddApplicationInsightsTelemetry();
            services.AddMemoryCache();

            // Transients
            services.AddTransient<IUserService, UserService>();
            services.AddTransient<IUserAccountsService, UserAccountsService>();
            services.AddTransient<IUserStateService, UserStateService>();
            services.AddTransient<IPlayerApi, PlayerApi>();
            services.AddTransient<IStationService, StationService>();
            services.AddTransient<IAccessTokenService, SpotifyAccessTokenService>();
            services.AddTransient<ICache, RingoMemoryCache>();
            
            services.AddTransient<ICosmosData<Models.User>, CosmosData<Models.User>>();
            services.AddTransient<ICosmosData<UserState>, CosmosData<UserState>>();
            services.AddTransient<ICosmosData<Station>, CosmosData<Station>>();
            services.AddTransient<ICosmosData<UserAccessToken>, CosmosData<UserAccessToken>>();

            // Singletons
            services.AddSingleton(new CosmosClient(Configuration["CosmosConnectionString"]));
            services.AddSingleton(new HttpClient());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
