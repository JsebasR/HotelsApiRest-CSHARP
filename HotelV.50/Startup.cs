using AspNetCoreRateLimit;
using Hoteles.Filters.Action;
using HotelV._50.Configurations;
using HotelV._50.Contracs;
using HotelV._50.Data.Context;
using HotelV._50.Filters;
using HotelV._50.Filters.Action;
using HotelV._50.Repository;
using HotelV._50.Services;
using HotelV._50.Services.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HotelV._50
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
            services.AddDbContext<DatabaseContext>(o =>
                o.UseSqlServer(Configuration.GetConnectionString("sqlConnection")));

            services.AddAuthentication();
            services.ConfigureIdentity();
            services.ConfigureJWT(Configuration);

            // Habilitar CORS
            services.AddCors(c =>
            {
                c.AddPolicy("CorsPolicy", builder =>
                    builder.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
            });

            //Automapper
            services.AddAutoMapper(typeof(MapperInitialize));

            //IUnitOfWork
            services.AddTransient<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IAuthManager, AuthManager>();

            //Filtro personalizado
            services.AddScoped<ValidateModelAttribute>();
            services.AddScoped<ValidateHotelExistsAttribute>();
            services.AddScoped<ValidateCountryExistsAttribute>();
            services.AddTransient<ValidationModel>();

            //Cache
            services.ConfigureResponseCaching();
            services.ConfigureHttpCacheHeaders();

            services.AddMemoryCache();

            //Rate
            services.ConfigureRateLimitingOptions();
            services.AddHttpContextAccessor();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Hotels by Country", Version = "v1" });

                c.SwaggerDoc("v2", new OpenApiInfo { Title = "Hotels by Country", Version = "v2" });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement()
                    {
                        {
                            new OpenApiSecurityScheme()
                            {
                                Reference = new OpenApiReference()
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer"
                                }
                            },
                            new string[]{}
                        }
                    }
                );
            });

            //Newtonsoft
            services.AddControllers(config =>
            {
                config.CacheProfiles.Add("120SecondsDuration", new CacheProfile()
                {
                    Duration = 120
                });
            }
            ).AddNewtonsoftJson(options => options.SerializerSettings.ReferenceLoopHandling =
                    Newtonsoft.Json.ReferenceLoopHandling.Ignore);

            // Versionamiento de la API
            services.ConfigureVersioning();

            //services.AddControllers(); 
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(s =>
                {
                    s.SwaggerEndpoint("/swagger/v1/swagger.json", "Hotel Listing API v1");
                    s.SwaggerEndpoint("/swagger/v2/swagger.json", "Hotel Listing API v2");
                });
            }
            

            app.ConfigureExceptionHandler();

            app.UseHttpsRedirection();

            //CORS
            app.UseCors("CorsPolicy");

            //Cache
            app.UseResponseCaching();
            app.UseHttpCacheHeaders();

            //app.UseIpRateLimiting();

            app.UseRouting();

            //Identity
            app.UseAuthentication();
            app.UseAuthorization();

            // Custom Middlewares
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}