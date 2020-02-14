using System.Threading.Tasks;
using WebApiTemplate.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Linq;

namespace WebApiTemplate
{
    public class Startup
    {
        private IConfiguration Configuration { get; }
        private readonly AppSettings settings;
        private const string AuthenticationName = "Bearer";
        private const string ServiceName = "WebApiTemplate";
        private const string Version = "v1";

        public Startup(IWebHostEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                            .SetBasePath(env.ContentRootPath)
                            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                            .AddEnvironmentVariables();

            Configuration = builder.Build();

            //Extract the AppSettings information from the appsettings config.
            settings = new AppSettings();
            Configuration.GetSection(nameof(AppSettings)).Bind(settings);
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //HealthChecks
            services.AddHealthChecks();

            services.AddControllers();

            services.AddAuthorization();
            services.AddAuthentication(AuthenticationName)
                .AddJwtBearer(AuthenticationName, options =>
                {
                    options.Authority = settings.AuthorizationServer;
                    options.RequireHttpsMetadata = false;
                    options.Audience = ServiceName;
                });

            services.AddLogging();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc(Version, new OpenApiInfo { Title = ServiceName, Version = Version });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                //Note: Comment as needed.
                app.UseHttpsRedirection();
            }

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.UseHealthChecks("/healthcheck", new HealthCheckOptions()
            {
                ResponseWriter = WriteResponse
            });

            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("swagger/v1/swagger.json", $"{ServiceName} API ({Version})");
                c.RoutePrefix = string.Empty;
            });
        }

        private static Task WriteResponse(HttpContext context, HealthReport report)
        {
            context.Response.ContentType = "application/json";

            var json = new JObject(
                        new JProperty("statusCode", report.Status),
                        new JProperty("status", report.Status.ToString()),
                        new JProperty("timelapsed", report.TotalDuration)
                );

            return context.Response.WriteAsync(json.ToString(Newtonsoft.Json.Formatting.Indented));
        }
    }
}
