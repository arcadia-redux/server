using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Server.Models;

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
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(Configuration.GetConnectionString("Database"));
                if (HostingEnvironment.IsDevelopment()) options.EnableSensitiveDataLogging();
            });
            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1)
                .AddRazorPagesOptions(o => o.Conventions.ConfigureFilter(new IgnoreAntiforgeryTokenAttribute()));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, AppDbContext dbContext)
        {
            dbContext.Database.Migrate();

            if (HostingEnvironment.IsDevelopment())
            {
                app.UseWhen(context => context.Request.Path.StartsWithSegments("/api"), appBuilder =>
                {
                    app.Use(async (context, next) =>
                    {
                        try
                        {
                            await next();
                        }
                        catch (Exception ex)
                        {
                            if (context.Response.HasStarted)
                            {
                                throw;
                            }

                            context.Response.StatusCode = 500;
                            context.Response.ContentType = "application/json";
                            var error = new
                            {
                                type = ex.GetType().FullName,
                                message = ex.Message,
                                stack = ex.StackTrace.Replace("\r\n", "\n")
                            };
                            await context.Response.WriteAsync(JToken.FromObject(error).ToString());
                        }
                    });
                });
                app.UseWhen(context => !context.Request.Path.StartsWithSegments("/api"),
                    appBuilder => { app.UseDeveloperExceptionPage(); });
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
