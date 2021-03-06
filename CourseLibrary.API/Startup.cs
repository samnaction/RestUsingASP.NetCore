namespace CourseLibrary.API
{
    using AutoMapper;
    using CourseLibrary.API.DbContexts;
    using CourseLibrary.API.Helpers;
    using CourseLibrary.API.Services;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Formatters;
    using Microsoft.AspNetCore.Mvc.Infrastructure;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Newtonsoft.Json.Serialization;
    using System;
    using System.Linq;

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
            services.AddResponseCaching();

            services.AddControllers(setupAction =>
           {
               setupAction.ReturnHttpNotAcceptable = true;
               setupAction.CacheProfiles.Add("240SecondsCacheProfile", new CacheProfile() { Duration = 240 });
           })
                .AddNewtonsoftJson(setupAction => setupAction.SerializerSettings.ContractResolver = 
                new CamelCasePropertyNamesContractResolver())
                .AddXmlDataContractSerializerFormatters()
                .ConfigureApiBehaviorOptions(setupAction =>
                {
                    setupAction.InvalidModelStateResponseFactory = context =>
                    {
                        var problemDetailsFactory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
                        var problemDetails = problemDetailsFactory.CreateValidationProblemDetails(context.HttpContext, context.ModelState);

                        problemDetails.Detail = "See the errors field for details";
                        problemDetails.Instance = context.HttpContext.Request.Path;

                        var actionExecutingContext = context as Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext;

                        if ((context.ModelState.ErrorCount > 0) &&
                        (actionExecutingContext?.ActionArguments.Count == context.ActionDescriptor.Parameters.Count))
                        {
                            problemDetails.Type = "https://courselibrary.com/modelvalidationproblem";
                            problemDetails.Status = StatusCodes.Status422UnprocessableEntity;
                            problemDetails.Title = "One or more validation errors occured.";

                            return new UnprocessableEntityObjectResult(problemDetails)
                            {
                                ContentTypes = { "application/problem+json" }
                            };
                        }

                        problemDetails.Status = StatusCodes.Status400BadRequest;
                        problemDetails.Title = "One or more errors on input occured.";
                        return new UnprocessableEntityObjectResult(problemDetails)
                        {
                            ContentTypes = { "application/problem+json" }
                        };
                    };
                });

            services.Configure<MvcOptions>( config => {
                var newtonsoftJsonOutputFormatter = config.OutputFormatters.OfType<NewtonsoftJsonOutputFormatter>()?
                .FirstOrDefault();
                if(newtonsoftJsonOutputFormatter != null)
                {
                    newtonsoftJsonOutputFormatter.SupportedMediaTypes.Add(Const.HateoasJson);
                }
            });

            //register PropertyCheckerService
            services.AddTransient<IPropertyCheckerService, PropertyCheckerService>();

            //register ProprtyMappingService
            services.AddTransient<IPropertyMappingService, PropertyMappingService>();

            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            services.AddScoped<ICourseLibraryRepository, CourseLibraryRepository>();
            services.AddDbContext<CourseLibraryContext>(options =>
           {
               options.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=CourseLibraryDB;Trusted_Connection=True;");
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
                app.UseExceptionHandler(appBuilder =>
               {
                   appBuilder.Run(async context =>
                   {
                       context.Response.StatusCode = 500;
                       await context.Response.WriteAsync("An unexpected fault happened. Try again Later");
                   });
               });
            }

            //app.UseHttpsRedirection();

            app.UseResponseCaching(); //need to be added before routing

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
