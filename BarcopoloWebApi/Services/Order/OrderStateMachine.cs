using System;
using BarcopoloWebApi.Entities;
using BarcopoloWebApi.Enums;
using BarcopoloWebApi.Exceptions;

namespace Domain.Orders
{
    public class OrderStateMachine
    {
        public bool TryChangeStatus(Order order, OrderStatus newStatus, bool isPrivileged, bool isDriver, bool isOwner)
        {
            var currentStatus = order.Status;

            if ((int)newStatus < (int)currentStatus && !isPrivileged)
                throw new InvalidOperationException("امکان بازگشت وضعیت وجود ندارد.");

            if (currentStatus == newStatus)
                return false;

            if (!isPrivileged)
            {
                if (isDriver)
                {
                    if (!IsDriverStatusTransitionAllowed(currentStatus, newStatus))
                        throw new UnauthorizedAccessAppException("شما مجاز به این تغییر وضعیت نیستید.");
                }
                else if (isOwner)
                {
                    if (newStatus != OrderStatus.Delivered)
                        throw new UnauthorizedAccessAppException("شما فقط مجاز به تغییر وضعیت به 'Delivered' هستید.");
                }
                else
                {
                    throw new UnauthorizedAccessAppException("شما مجاز به تغییر وضعیت این سفارش نیستید.");
                }
            }

            if (newStatus == OrderStatus.Cancelled && (int)currentStatus >= (int)OrderStatus.Assigned && !isPrivileged)
                throw new UnauthorizedAccessAppException("شما مجاز به لغو سفارش در این وضعیت نیستید.");

            order.Status = newStatus;

            if (newStatus == OrderStatus.Delivered && order.DeliveryTime == null)
                order.DeliveryTime = DateTime.UtcNow;

            return true;
        }

        private bool IsDriverStatusTransitionAllowed(OrderStatus current, OrderStatus next)
        {
            return (current == OrderStatus.Assigned && next == OrderStatus.Loading)
                   || (current == OrderStatus.Loading && next == OrderStatus.InProgress)
                   || (current == OrderStatus.InProgress && next == OrderStatus.Unloading);
        }
    }
}