using BarcopoloWebApi.Data;
using BarcopoloWebApi.Entities;
using BarcopoloWebApi.Enums;
using BarcopoloWebApi.Exceptions;
using Microsoft.EntityFrameworkCore;

public static class OrderAccessGuard
{
    public static async Task EnsureUserCanAccessOrderAsync(Order order, Person user, DataBaseContext context, long? resourceOwnerId = null)
    {
        if (user == null)
            throw new NotFoundException("کاربر یافت نشد.");

        if (order == null)
            throw new NotFoundException("سفارش یافت نشد.");

        if (user.IsAdminOrSuperAdminOrMonitor())
            return;

        if (order.OrganizationId == null)
        {
            if (order.OwnerId != user.Id)
                throw new ForbiddenAccessException("شما به این سفارش شخصی دسترسی ندارید.");

            return;
        }

        if (resourceOwnerId.HasValue && resourceOwnerId.Value == user.Id)
            return;

        if (order.BranchId.HasValue)
        {
            var isBranchMember = await context.OrganizationMemberships.AnyAsync(m =>
                m.OrganizationId == order.OrganizationId &&
                m.BranchId == order.BranchId &&
                m.PersonId == user.Id);

            if (!isBranchMember)
                throw new ForbiddenAccessException("شما عضو شعبه‌ای که سفارش را ثبت کرده نیستید.");
        }
        else 
        {
            var isOrgMember = await context.OrganizationMemberships.AnyAsync(m =>
                m.OrganizationId == order.OrganizationId &&
                m.BranchId == null &&
                m.PersonId == user.Id);

            if (!isOrgMember)
                throw new ForbiddenAccessException("شما عضو سازمانی که سفارش را ثبت کرده نیستید.");
        }
    }
    public static async Task EnsureUserCanAccessOrderEventAsync(Order order, Person user, DataBaseContext context, long? resourceOwnerId = null)
    {
        if (user == null)
            throw new NotFoundException("کاربر یافت نشد.");

        if (order == null)
            throw new NotFoundException("سفارش یافت نشد.");

        if (user.IsAdminOrSuperAdminOrMonitor())
            return;

        if (resourceOwnerId.HasValue && resourceOwnerId.Value == user.Id)
            return;

        var isAssignedDriver = await context.OrderVehicles
            .Include(ov => ov.Vehicle)
            .AnyAsync(ov => ov.OrderId == order.Id && ov.Vehicle.Driver.PersonId == user.Id);

        if (isAssignedDriver)
            return;

        throw new ForbiddenAccessException("شما به این سفارش دسترسی ندارید.");
    }


}