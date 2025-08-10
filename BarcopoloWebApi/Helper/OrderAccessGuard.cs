using BarcopoloWebApi.Data;
using BarcopoloWebApi.Entities;
using BarcopoloWebApi.Enums;
using BarcopoloWebApi.Exceptions;
using Microsoft.EntityFrameworkCore;

public static class OrderAccessGuard
{
    //public static async Task EnsureUserCanAccessOrderAsync(Order order, Person user, DataBaseContext context, long? resourceOwnerId = null)
    //{
    //    var isAdmin = user.IsAdminOrSuperAdminOrMonitor();
    //    var isOwner = resourceOwnerId.HasValue && resourceOwnerId.Value == user.Id;

    //    if (isAdmin || isOwner)
    //        return;

    //    if (order.OrganizationId == null)
    //    {
    //        if (order.OwnerId != user.Id)
    //            throw new UnauthorizedAccessAppException("شما به این سفارش دسترسی ندارید.");
    //    }
    //    else
    //    {
    //        if (order.BranchId != null)
    //        {
    //            var isBranchMember = await context.OrganizationMemberships.AnyAsync(m =>
    //                m.OrganizationId == order.OrganizationId &&
    //                m.BranchId == order.BranchId &&
    //                m.PersonId == user.Id);

    //            if (!isBranchMember)
    //                throw new UnauthorizedAccessAppException("شما عضو شعبه سفارش نیستید.");
    //        }
    //        else
    //        {
    //            var isOrgMember = await context.OrganizationMemberships.AnyAsync(m =>
    //                m.OrganizationId == order.OrganizationId &&
    //                m.PersonId == user.Id);

    //            if (!isOrgMember)
    //                throw new UnauthorizedAccessAppException("شما عضو سازمان سفارش نیستید.");
    //        }
    //    }
    //}

    public static async Task EnsureUserCanAccessOrderAsync(Order order, Person user, DataBaseContext context, long? resourceOwnerId = null)
    {
        if (user == null)
            throw new NotFoundException("کاربر یافت نشد.");

        if (order == null)
            throw new NotFoundException("سفارش یافت نشد.");

        if (user.IsAdminOrSuperAdminOrMonitor())
            return;

        // If it's personal order (not organizational)
        if (order.OrganizationId == null)
        {
            if (order.OwnerId != user.Id)
                throw new ForbiddenAccessException("شما به این سفارش شخصی دسترسی ندارید.");

            return;
        }

        // If specific resource owner is passed (e.g. Feedback created by a user)
        if (resourceOwnerId.HasValue && resourceOwnerId.Value == user.Id)
            return;

        // If order is from an organization with a specific branch
        if (order.BranchId.HasValue)
        {
            var isBranchMember = await context.OrganizationMemberships.AnyAsync(m =>
                m.OrganizationId == order.OrganizationId &&
                m.BranchId == order.BranchId &&
                m.PersonId == user.Id);

            if (!isBranchMember)
                throw new ForbiddenAccessException("شما عضو شعبه‌ای که سفارش را ثبت کرده نیستید.");
        }
        else // Organization without branches
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

        // اجازه به resource owner (مثل feedback creator)
        if (resourceOwnerId.HasValue && resourceOwnerId.Value == user.Id)
            return;

        // اگر سفارش شخصی است
        //if (order.OrganizationId == null)
        //{
        //    if (order.OwnerId != user.Id)
        //        throw new ForbiddenAccessException("شما به این سفارش شخصی دسترسی ندارید.");

        //    return;
        //}

        //// اگر سفارش سازمانی دارای شعبه است
        //if (order.BranchId.HasValue)
        //{
        //    var isBranchMember = await context.OrganizationMemberships.AnyAsync(m =>
        //        m.OrganizationId == order.OrganizationId &&
        //        m.BranchId == order.BranchId &&
        //        m.PersonId == user.Id);

        //    if (isBranchMember)
        //        return;
        //}
        //else // سفارش سازمانی بدون شعبه
        //{
        //    var isOrgMember = await context.OrganizationMemberships.AnyAsync(m =>
        //        m.OrganizationId == order.OrganizationId &&
        //        m.BranchId == null &&
        //        m.PersonId == user.Id);

        //    if (isOrgMember)
        //        return;
        //}

        // ✅ اگر راننده به سفارش اختصاص داده شده باشد
        var isAssignedDriver = await context.OrderVehicles
            .Include(ov => ov.Vehicle)
            .AnyAsync(ov => ov.OrderId == order.Id && ov.Vehicle.Driver.PersonId == user.Id);

        if (isAssignedDriver)
            return;

        throw new ForbiddenAccessException("شما به این سفارش دسترسی ندارید.");
    }


}