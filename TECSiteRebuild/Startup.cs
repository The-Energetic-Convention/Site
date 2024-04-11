//using section
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net;
using TECSite.Controllers;
using System.Runtime.CompilerServices;

namespace TECsite
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
            services.AddControllersWithViews();

            services.AddMvc();

            services.AddHttpContextAccessor();

            services.AddHsts(options =>
            {
                options.Preload = true;
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromHours(2);
            });

            /*
            services.AddSession(options =>
            {
                // Set a short timeout for easy testing.
                options.IdleTimeout = TimeSpan.FromSeconds(10);
                options.Cookie.HttpOnly = false;
            });
            */
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.Use((context, next) =>
            {
                context.Response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");
                context.Response.Headers.Add("Expires", "0");
                context.Response.Headers.Add("Pragma", "no-cache");
                return next.Invoke();
            });

            app.UseExceptionHandler("/Home/Error");

            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();

            //app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();

                endpoints.MapControllerRoute(
                    name: "events",
                    pattern: "{controller=Events}/{action=Event}/{id?}");

                endpoints.MapControllerRoute(
                    name: "currentevent",
                    pattern: "{controller=Events}/{action=Current}/{id?}");

                endpoints.MapControllerRoute(
                    name: "currentuser",
                    pattern: "{controller=Users}/{action=Me}/{id?}");

                endpoints.MapControllerRoute(
                    name: "users",
                    pattern: "{controller=Users}/{action=User}/{id?}");

                endpoints.MapControllerRoute(
                    name: "api",
                    pattern: "{controller=Api}/{action=Index}/{id?}");

                endpoints.MapFallbackToController("PageNotFound", "Home");
            });
            
        }
    }
}