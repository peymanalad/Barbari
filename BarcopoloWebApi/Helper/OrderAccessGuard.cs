using BarcopoloWebApi.Data;
using BarcopoloWebApi.Entities;
using BarcopoloWebApi.Enums;
using Microsoft.EntityFrameworkCore;

public static class OrderAccessGuard
{
    public static async Task EnsureUserCanAccessOrderAsync(Order order, Person user, DataBaseContext context, long? resourceOwnerId = null)
    {
        var isAdmin = user.IsAdminOrSuperAdmin();
        var isOwner = resourceOwnerId.HasValue && resourceOwnerId.Value == user.Id;

        if (isAdmin || isOwner)
            return;

        if (order.OrganizationId == null)
        {
            if (order.OwnerId != user.Id)
                throw new UnauthorizedAccessAppException("شما به این سفارش دسترسی ندارید.");
        }
        else
        {
            if (order.BranchId != null)
            {
                var isBranchMember = await context.OrganizationMemberships.AnyAsync(m =>
                    m.OrganizationId == order.OrganizationId &&
                    m.BranchId == order.BranchId &&
                    m.PersonId == user.Id);

                if (!isBranchMember)
                    throw new UnauthorizedAccessAppException("شما عضو شعبه سفارش نیستید.");
            }
            else
            {
                var isOrgMember = await context.OrganizationMemberships.AnyAsync(m =>
                    m.OrganizationId == order.OrganizationId &&
                    m.PersonId == user.Id);

                if (!isOrgMember)
                    throw new UnauthorizedAccessAppException("شما عضو سازمان سفارش نیستید.");
            }
        }
    }
}