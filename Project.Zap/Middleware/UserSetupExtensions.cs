using Microsoft.AspNetCore.Builder;
using System.Threading.Tasks;

namespace Project.Zap.Middleware
{
    public static class UserSetupExtensions
    {
        public static IApplicationBuilder UseUserRegistration(this IApplicationBuilder app)
        {
            app.Use((context, next) =>
            {
                if (context.User.HasClaim(c => c.Type == "newUser" && c.Value == "true") && (!context.Request.Path.Value.Contains("/NewUser") && !context.Request.Path.Value.Contains("/AzureADB2C/Account")))
                {
                    context.Response.StatusCode = 302;
                    context.Response.Headers["Location"] = "/NewUser";
                    return Task.CompletedTask;
                }
                return next();
            });

            return app;
        }
        
    }
}
