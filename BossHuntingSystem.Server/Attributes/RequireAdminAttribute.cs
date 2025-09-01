using Microsoft.AspNetCore.Authorization;

namespace BossHuntingSystem.Server.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RequireAdminAttribute : AuthorizeAttribute
    {
        public RequireAdminAttribute() : base("Admin")
        {
            Roles = "Admin";
        }
    }
}


