using Microsoft.AspNetCore.Authorization;

namespace Ecom.API.Authorization
{
    public class OrderOwnerOrAdminRequirement : IAuthorizationRequirement
    {
    }
}