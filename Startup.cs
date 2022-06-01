using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kryxivia.AuthLoaderAPI.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using Kryxivia.Domain.Extensions;
using Serilog;
using Kryxivia.AuthLoaderAPI.Middlewares;
using Kryxivia.Contracts.Options;
using Microsoft.DotNet.PlatformAbstractions;
using Kryxivia.Shared.Settings;
using System.Reflection;
using System.IO;
using Kryxivia.AuthLoaderAPI.Middlewares.Attributes;
using Kryxivia.AuthLoaderAPI.HealthChecks;
using Kryxivia.AuthLoaderAPI.Services.LoginQueue;
using Kryxivia.AuthLoaderAPI.Utilities;
using Kryxivia.AuthLoaderAPI.Filters;

namespace Kryxivia.AuthLoaderAPI
{
    public class Startup
    {
        private readonly IWebHostEnvironment WebHostEnvironment;

        public Startup(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            Configuration = configuration;
            WebHostEnvironment = webHostEnvironment;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Configuration...
            var web3Section = Configuration.GetSection("Web3");
            services.Configure<Web3Settings>(web3Section);

            services.Configure<JwtSettings>(Configuration.GetSection("Jwt"));
            services.Configure<SwaggerSettings>(Configuration.GetSection("Swagger"));
            services.Configure<LoginQueueSettings>(Configuration.GetSection("LoginQueue"));

            // Kryxivia MongoDB...
            services.AddKryxMongoDBWithRepositories(Configuration.GetConnectionString("KryxiviaDatabase"));

            // Application Insights...
            services.AddApplicationInsightsTelemetry();

            // Kryxivia Contracts...
            var web3Settings = web3Section.Get<Web3Settings>();
            services.AddKryxContracts(options =>
            {
                if (!string.IsNullOrWhiteSpace(web3Settings.Testnet?.NftContractAddr))
                {
                    options.TestnetWeb3 = web3Settings.TestnetWeb3();
                    options.TestnetNftContractAddress = web3Settings.Testnet.NftContractAddr;
                }

                if (!string.IsNullOrWhiteSpace(web3Settings.Mainnet?.NftContractAddr))
                {
                    options.MainnetWeb3 = web3Settings.MainnetWeb3();
                    options.MainnetNftContractAddress = web3Settings.Mainnet.NftContractAddr;
                }
            });

            // Services...
            services.AddSingleton<LoginQueueService>();

            // Health Checks...
            services.AddHealthChecks()
                .AddCheck<MongoDBHealthCheck>("MongoDB");

            // Swagger...
            services.AddSwaggerGen(s =>
            {
                s.SwaggerDoc("v1", new OpenApiInfo() { Title = "Kryxivia.AuthLoaderAPI", Version = "v1" });
                s.OperationFilter<AuthorizeCheckOperationFilter>();
                s.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header. \r\n\r\n Enter your token in the text input below.\r\n\r\nExample: \" 12345abcdef\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                s.IncludeXmlComments(xmlPath);
            });

            // Controllers...
            services.AddControllers();

            // Cors...
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder.SetIsOriginAllowed(_ => true).AllowAnyMethod().AllowAnyHeader().AllowCredentials();
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // Serilog...
            app.UseSerilogRequestLogging();

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseCors();

            app.UseAuthentication();
            app.UseAuthorization();

            // Middlewares...
            app.UseMiddleware<JwtMiddleware>();
            app.UseMiddleware<SwaggerBasicAuthMiddleware>();

            // Swagger...
            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health");
                endpoints.MapControllers();
            });
        }
    }
}
