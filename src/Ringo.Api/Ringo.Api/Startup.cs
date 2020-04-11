using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
            services.AddControllers();
            services.AddApplicationInsightsTelemetry();

            services.AddSingleton<IUserService, UserService>();
            services.AddSingleton<IUserAccountsService, UserAccountsService>();
            services.AddSingleton<IUserStateService, UserStateService>();
            services.AddSingleton<ICosmosData<Models.User>, CosmosData<Models.User>>();
            services.AddSingleton<ICosmosData<UserState>, CosmosData<UserState>>();
            services.AddSingleton<ICosmosData<Station>, CosmosData<Station>>();
            services.AddSingleton<IPlayerApi>(new PlayerApi(new HttpClient()));
            services.AddSingleton(new CosmosClient(Configuration["CosmosConnectionString"]));
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
