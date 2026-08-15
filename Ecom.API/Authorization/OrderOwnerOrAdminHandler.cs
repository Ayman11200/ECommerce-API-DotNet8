using Ecom.Core.Entities.Order;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Ecom.API.Authorization
{
    public class OrderOwnerOrAdminHandler
        : AuthorizationHandler<OrderOwnerOrAdminRequirement, Order>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            OrderOwnerOrAdminRequirement requirement,
            Order resource)
        {
            if (context.User.IsInRole("Admin"))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            var email = context.User.FindFirstValue(ClaimTypes.Email);

            if (email is not null && email == resource.BuyerEmail)
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}