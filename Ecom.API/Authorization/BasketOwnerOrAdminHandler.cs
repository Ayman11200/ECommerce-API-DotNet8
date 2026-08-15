using Ecom.API.Extensions;
using Ecom.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Ecom.API.Authorization
{
    public class BasketOwnerOrAdminHandler
        : AuthorizationHandler<BasketOwnerOrAdminRequirement, CustomerBasket>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            BasketOwnerOrAdminRequirement requirement,
            CustomerBasket resource)
        {
         
            if (context.User.IsInRole("Admin"))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

 
            if (resource.OwnerId is null)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

           
            var userId = context.User.GetUserId();

            if (userId is not null && userId == resource.OwnerId)
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}