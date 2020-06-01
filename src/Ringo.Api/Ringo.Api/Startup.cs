using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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
            services.AddCors(options =>
            {
                options.AddPolicy(name: "The Corrs",
                                  builder =>
                                  {
                                      builder
                                      .WithOrigins("http://localhost:8080")
                                      .AllowCredentials()
                                      .AllowAnyHeader()
                                      .AllowAnyMethod();
                                  });
            });

            services.AddControllers(options => options.Filters.Add<AuthSpotifyBearerFilter>());
            services.AddApplicationInsightsTelemetry();
            services.AddMemoryCache();

            // Transients
            services.AddTransient<IUserService, UserService>();
            services.AddTransient<IUserAccountsService, UserAccountsService>();
            services.AddTransient<IUserStateService, UserStateService>();
            services.AddTransient<IStationService, StationService>();
            services.AddTransient<IPlayerService, PlayerService>();
            services.AddTransient<IAccessTokenService, AccessTokenService>();
            services.AddTransient<ICache, RingoMemoryCache>();
            services.AddTransient<IPlayerApi, PlayerApi>();

            services.AddTransient<ICosmosData<Models.User>, CosmosData<Models.User>>();
            services.AddTransient<ICosmosData<UserState>, CosmosData<UserState>>();
            services.AddTransient<ICosmosData<Station>, CosmosData<Station>>();
            services.AddTransient<ICosmosData<UserAccessRefreshToken>, CosmosData<UserAccessRefreshToken>>();
            services.AddTransient<ICosmosData<Player>, CosmosData<Player>>();

            // Singletons
            services.AddSingleton(new CosmosClient(Configuration["CosmosConnectionString"]));
            services.AddSingleton(new HttpClient());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            //if (env.IsDevelopment())
            //{
            //    //app.UseDeveloperExceptionPage();
            //}

            app.UseExceptionHandler(new ExceptionHandlerOptions
            {
                ExceptionHandler = new JsonExceptionMiddleware(env.IsDevelopment()).Invoke
            });

            app.UseHttpsRedirection();

            // order is important
            app.UseRouting();
            app.UseCors("The Corrs");
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
