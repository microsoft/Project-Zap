using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureADB2C.UI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;
using Project.Zap.Filters;
using Project.Zap.Library.Models;
using Project.Zap.Library.Services;
using Project.Zap.Middleware;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Project.Zap
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
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential 
                // cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                // requires using Microsoft.AspNetCore.Http;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddAuthentication(AzureADB2CDefaults.AuthenticationScheme)
                .AddAzureADB2C(options => Configuration.Bind("AzureAdB2C", options));

            services.AddAuthorization(options =>
            {
                options.AddPolicy("OrgAManager", policy =>
                   policy.RequireAssertion(context =>
                    context.User.HasClaim(c => c.Type == "extension_zaprole" && c.Value == "org_a_manager")));

                options.AddPolicy("OrgBEmployee", policy =>
                  policy.RequireAssertion(context =>
                   context.User.HasClaim(c => c.Type == "extension_zaprole" && c.Value == "org_b_employee")));

                options.AddPolicy("ShiftViewer", policy =>
                  policy.RequireAssertion(context =>
                   context.User.HasClaim(c => c.Type == "extension_zaprole" && c.Value == "org_a_manager" || c.Type == "extension_zaprole" && c.Value == "org_b_employee")));
            });

            services.AddLocalization(options => options.ResourcesPath = "Resources");
            services.AddControllers().AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
               .AddDataAnnotationsLocalization();
            services.AddRazorPages();

            services.AddSingleton<IValidationAttributeAdapterProvider, DateLessThanAttributeAdapterProvider>();

            services.AddTransient<Database>(x => this.GetCosmosDatabase().Result);
            
            services.AddSingleton<IRepository<Library.Models.Organization>, OrganizationRepository>();
            services.AddSingleton<IRepository<Library.Models.Shift>, ShiftRepository>();
            services.AddSingleton<IRepository<PartnerOrganization>, PartnerRepository>();
            services.AddSingleton<IRepository<Employee>, EmployeeRepository>();

            services.AddTransient<IConfidentialClientApplication>(x => ConfidentialClientApplicationBuilder
                .Create(this.Configuration["ClientId"])
                .WithTenantId(this.Configuration["TenantId"])
                .WithClientSecret(this.Configuration["ClientSecret"])
                .Build());
            services.AddTransient<IAuthenticationProvider, ClientCredentialProvider>();
            services.AddSingleton<IGraphServiceClient, GraphServiceClient>();

            services.Configure<RequestLocalizationOptions>(options =>
            {
                var supportedCultures = new List<CultureInfo>
                    {
                        new CultureInfo("en-US"),
                        new CultureInfo("fr")
                    };

                options.DefaultRequestCulture = new RequestCulture("en-US");
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;
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
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            var supportedCultures = new[]
            {
                new CultureInfo("en-US"),
                new CultureInfo("fr"),
            };

            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("en-US"),                
                SupportedCultures = supportedCultures,                
                SupportedUICultures = supportedCultures
            });

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseRouting();
            
            app.UseAuthentication();
            app.UseUserRegistration(this.Configuration["ManagerCode"], this.Configuration["ExtensionId"]);           
            app.UseAuthorization();
            
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();                   
            });
        }

        private async Task<Database> GetCosmosDatabase()
        {
            string endpoint = this.Configuration["CosmosEndpoint"];
            string key = this.Configuration["CosmosKey"];
            CosmosClient client = new CosmosClientBuilder(endpoint, key).Build();            
            return await client.CreateDatabaseIfNotExistsAsync("zap");
        }
    }
}
