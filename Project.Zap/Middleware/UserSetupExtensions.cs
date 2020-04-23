using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using Project.Zap.Library.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Project.Zap.Library.Services;
using System.Threading;
using System.Threading.Tasks;

namespace Project.Zap.Middleware
{
    public static class UserSetupExtensions
    {
        public static IApplicationBuilder UseUserRegistration(this IApplicationBuilder app, string managerCode, string extensionId)
        {
            app.Use(async (context, next) =>
            {
                if (context.User.HasClaim(c => c.Type == "newUser" && c.Value == "true" && !context.User.HasClaim(c => c.Type == "extension_zaprole")))
                {
                    IGraphServiceClient graphClient = app.ApplicationServices.GetService<IGraphServiceClient>();

                    Claim id = context.User.Claims.Where(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").FirstOrDefault();
                    if (id == null)
                    {
                        throw new ArgumentException("http://schemas.microsoft.com/identity/claims/objectidentifier claim is required on new user signin");
                    }

                    Claim registrationCode = context.User.Claims.Where(x => x.Type == "extension_RegistrationCode").FirstOrDefault();
                    if (registrationCode == null)
                    {
                        await graphClient.Users[id.Value].Request().DeleteAsync();
                        throw new ArgumentException("Registration Code is required on new user signin");
                    }               

                    if(managerCode == registrationCode.Value)
                    {
                        context.User = await UpdateUserRole(context.User, "org_a_manager", id.Value, extensionId, graphClient);
                    }
                    else
                    {
                        IRepository<PartnerOrganization> partnerRepository = app.ApplicationServices.GetService<IRepository<PartnerOrganization>>();

                        PartnerOrganization partner = (await partnerRepository.Get(
                            "SELECT * FROM c WHERE c.RegistrationCode = @registrationCode",
                            new Dictionary<string, object> { { "@registrationCode", registrationCode.Value } })).FirstOrDefault();

                        if (partner == null)
                        {
                            await graphClient.Users[id.Value].Request().DeleteAsync();
                            throw new ArgumentException("No Partner organization found");
                        }

                        context.User = await UpdateUserRole(context.User, "org_b_employee", id.Value, extensionId, graphClient);
                    }                    
                }

                await next.Invoke();
            });

            return app;
        }

        private static async Task<ClaimsPrincipal> UpdateUserRole(ClaimsPrincipal user, string role, string id, string extensionId, IGraphServiceClient graphClient)
        {
            var claims = user.Claims.Append(new Claim("extension_zaprole", role));
            ClaimsPrincipal principal = new ClaimsPrincipal(new ClaimsIdentity(user.Identity, claims));
            
            Thread.CurrentPrincipal = principal;

            IDictionary<string, object> extensions = new Dictionary<string, object>();
            extensions.Add($"extension_{extensionId}_zaprole", role);

            var adUser = new Microsoft.Graph.User
            {
                AdditionalData = extensions
            };

            int retries = 0;
            async Task updateUser(string userId)
            {
                try
                {
                    await graphClient.Users[userId].Request().UpdateAsync(adUser);
                }
                catch(Exception exception)
                {
                    await Task.Delay(1000);
                    retries++;
                    if (retries > 2) throw exception;
                    
                    await updateUser(id);
                }
            }

            await updateUser(id);

            return principal;
        }
    }
}
