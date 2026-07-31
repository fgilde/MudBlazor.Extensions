namespace TryMudEx.Server
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.HttpsPolicy;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using MudBlazor.Examples.Data;
    using Playzor.Server;

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
            services.AddMemoryCache();
            services.AddSingleton<IndexHtmlService>();
            services.AddScoped<IPeriodicTableService, PeriodicTableService>();
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    builder =>
                    {
                        builder.AllowAnyOrigin()
                                    .AllowAnyMethod()
                                    .AllowAnyHeader();
                        //builder.WithOrigins("https://mudex.org", "https://www.mudex.org", "https://mudex.azurewebsites.net");
                    });

            });

            // the nuget proxy the playground browser needs comes from the Playzor.Server package;
            // foreign origins may use it so an app can point its editor here without hosting one
            services.AddPlayzorServer(o => o.AllowedOrigins.Add("*"));

            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseWebAssemblyDebugging();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseCors();

            if (env.IsDevelopment())
            {
                // rebuilt assemblies and edited wwwroot files must never come from the browser cache
                app.Use(async (context, next) =>
                {
                    context.Response.OnStarting(() =>
                    {
                        context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
                        return Task.CompletedTask;
                    });
                    await next();
                });
            }

            // Needed for wasm project
            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapPlayzorApi();

                // Serve the wasm project if no other matches — rendered so placeholders
                // (asset version, later brand tokens) get replaced per request
                endpoints.MapFallback(async context =>
                {
                    var indexHtml = context.RequestServices.GetRequiredService<IndexHtmlService>();
                    await indexHtml.WriteResponseAsync(context);
                });
            });



        }
    }
}
