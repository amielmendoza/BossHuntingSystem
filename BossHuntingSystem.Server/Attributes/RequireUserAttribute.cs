using Microsoft.AspNetCore.Authorization;

namespace BossHuntingSystem.Server.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RequireUserAttribute : AuthorizeAttribute
    {
        public RequireUserAttribute() : base("User")
        {
            Roles = "User,Admin";
        }
    }
}


