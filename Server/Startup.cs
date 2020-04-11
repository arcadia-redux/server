using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Server.Models;
using Server.Services;

namespace Server
{
    public class Startup
    {
        private IHostingEnvironment HostingEnvironment { get; }
        private IConfiguration Configuration { get; }

        public Startup(IHostingEnvironment env, IConfiguration configuration)
        {
            HostingEnvironment = env;
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<StripeService>();
            services.AddScoped<LeaderBoardService>();
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(Configuration.GetConnectionString("Database"));
                if (HostingEnvironment.IsDevelopment()) options.EnableSensitiveDataLogging();
            });

            services.AddAuthentication(options =>
            {
                options.DefaultScheme = DedicatedServerKeyAuthenticationOptions.DefaultScheme;
                options.DefaultAuthenticateScheme = DedicatedServerKeyAuthenticationOptions.DefaultScheme;
                options.DefaultChallengeScheme = DedicatedServerKeyAuthenticationOptions.DefaultScheme;
            }).AddScheme<DedicatedServerKeyAuthenticationOptions, DedicatedServerKeyAuthenticationHandler>(DedicatedServerKeyAuthenticationOptions.DefaultScheme, options => { });

            services.AddControllers();
            services.AddRazorPages()
                .AddRazorPagesOptions(o => o.Conventions.ConfigureFilter(new IgnoreAntiforgeryTokenAttribute()));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, AppDbContext dbContext)
        {
            dbContext.Database.Migrate();

            app.UseWhen(context => context.Request.Path.StartsWithSegments("/api"), appBuilder =>
            {
                app.UseExceptionHandler(a => a.Run(async context =>
                {
                    context.Response.StatusCode = 500;
                    context.Response.ContentType = "application/json";

                    var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
                    var exception = exceptionHandlerPathFeature.Error;
                    await context.Response.WriteAsync($"{exception.GetType().Name}: {exception.Message}");
                }));
            });

            app.UseStaticFiles();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages();
            });

            if (HostingEnvironment.IsDevelopment())
            {
                app.UseWhen(context => !context.Request.Path.StartsWithSegments("/api"),
                    appBuilder => { appBuilder.UseDeveloperExceptionPage(); });
            }
            else
            {
                app.UseHsts();
                app.UseHttpsRedirection();
            }
        }
    }
}
