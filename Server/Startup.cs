using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Server.Models;
using Server.Services;

namespace Server
{
    public class Startup
    {
        private IConfiguration Configuration { get; }
        private IWebHostEnvironment Environment { get; }

        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }

        private bool IsApiRoute(HttpContext context) => context.Request.Path.StartsWithSegments("/api");

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<StripeService>();
            services.AddScoped<RatingService>();
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(Configuration.GetConnectionString("Database"));
                if (Environment.IsDevelopment()) options.EnableSensitiveDataLogging();
            });

            services.AddAuthentication()
                .AddDedicatedServerKey()
                .AddAdminKey();

            services.AddAuthorization(options =>
            {
                options.AddPolicy("Lua", policy => policy
                    .AddAuthenticationSchemes(DedicatedServerKeyAuthenticationOptions.DefaultScheme)
                    .RequireAuthenticatedUser());

                options.AddPolicy("Admin", policy => policy
                    .AddAuthenticationSchemes(AdminKeyAuthenticationOptions.DefaultScheme)
                    .RequireAuthenticatedUser());
            });

            services.AddControllers()
                .AddInvalidModelStateLogging(IsApiRoute);

            services.AddRazorPages()
                .AddRazorPagesOptions(o => o.Conventions.ConfigureFilter(new IgnoreAntiforgeryTokenAttribute()));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, AppDbContext dbContext)
        {
            dbContext.Database.Migrate();

            app.UseWhen(IsApiRoute, a => a.UseJsonExceptionHandler());
            if (Environment.IsDevelopment())
                app.UseWhen(c => !IsApiRoute(c), a => a.UseDeveloperExceptionPage());

            app.UseStaticFiles();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages();
            });
        }
    }
}
