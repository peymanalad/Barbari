using BarcopoloWebApi.Data;
using BarcopoloWebApi.DTOs.Payment;
using BarcopoloWebApi.Entities;
using BarcopoloWebApi.Exceptions;
using BarcopoloWebApi.Helper;
using BarcopoloWebApi.Services;
using Microsoft.EntityFrameworkCore;

public class PaymentService : IPaymentService
{
    private readonly DataBaseContext _context;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(DataBaseContext context, ILogger<PaymentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PaymentDto> CreateAsync(CreatePaymentDto dto, long currentUserId)
    {
        _logger.LogInformation("در حال ثبت پرداخت جدید برای سفارش {OrderId} توسط کاربر {UserId}", dto.OrderId, currentUserId);

        var order = await _context.Orders
            .Include(o => o.Organization)
            .FirstOrDefaultAsync(o => o.Id == dto.OrderId)
            ?? throw new NotFoundException("سفارش یافت نشد.");

        var currentUser = await _context.Persons.FindAsync(currentUserId)
            ?? throw new NotFoundException("کاربر جاری یافت نشد.");

        await OrderAccessGuard.EnsureUserCanAccessOrderAsync(order, currentUser, _context);

        bool transactionExists = await _context.Payments.AnyAsync(p => p.TransactionId == dto.TransactionId);
        if (transactionExists)
            throw new InvalidOperationException("این شناسه تراکنش قبلاً ثبت شده است.");

        decimal total = order.Fare + order.Insurance + order.Vat;
        decimal paid = await _context.Payments
            .Where(p => p.OrderId == dto.OrderId)
            .SumAsync(p => p.Amount);
        decimal remaining = total - paid;
        if (dto.Amount > remaining)
            throw new BadRequestException("مبلغ پرداخت بیشتر از مبلغ باقی‌مانده سفارش است.");

        var paymentDate = dto.PaymentDate;
        if (!paymentDate.HasValue || paymentDate.Value == DateTime.MinValue)
            paymentDate = DateTime.UtcNow;


        var payment = new Payment
        {
            OrderId = dto.OrderId,
            PaymentMethod = dto.PaymentType,
            Amount = dto.Amount,
            PaymentDate = paymentDate.Value,
            TransactionId = dto.TransactionId
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        return MapToDto(payment);
    }

    public async Task<PaymentDto> GetByIdAsync(long id, long currentUserId)
    {
        _logger.LogInformation("دریافت پرداخت {PaymentId} توسط کاربر {UserId}", id, currentUserId);

        var payment = await _context.Payments
            .Include(p => p.Order).ThenInclude(o => o.Organization)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException("پرداخت یافت نشد.");

        var currentUser = await _context.Persons.FindAsync(currentUserId)
            ?? throw new NotFoundException("کاربر جاری یافت نشد.");

        await OrderAccessGuard.EnsureUserCanAccessOrderAsync(payment.Order, currentUser, _context);

        return MapToDto(payment);
    }

    public async Task<IEnumerable<PaymentDto>> GetByOrderIdAsync(long orderId, long currentUserId)
    {
        _logger.LogInformation("دریافت پرداخت‌های سفارش {OrderId} توسط کاربر {UserId}", orderId, currentUserId);

        var order = await _context.Orders
            .Include(o => o.Organization)
            .FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new NotFoundException("سفارش یافت نشد.");

        var currentUser = await _context.Persons.FindAsync(currentUserId)
            ?? throw new NotFoundException("کاربر جاری یافت نشد.");

        await OrderAccessGuard.EnsureUserCanAccessOrderAsync(order, currentUser, _context);

        var payments = await _context.Payments
            .Where(p => p.OrderId == orderId)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();

        return payments.Select(MapToDto).ToList();
    }

    public async Task<PaymentDto> UpdateAsync(long id, UpdatePaymentDto dto, long currentUserId)
    {
        _logger.LogInformation("ویرایش پرداخت {PaymentId} توسط کاربر {UserId}", id, currentUserId);

        var payment = await _context.Payments
            .Include(p => p.Order).ThenInclude(o => o.Organization)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException("پرداخت یافت نشد.");

        var currentUser = await _context.Persons.FindAsync(currentUserId)
            ?? throw new NotFoundException("کاربر جاری یافت نشد.");

        await OrderAccessGuard.EnsureUserCanAccessOrderAsync(payment.Order, currentUser, _context);

        if (dto.PaymentType.HasValue)
            payment.PaymentMethod = dto.PaymentType.Value;
        if (dto.Amount.HasValue)
        {
            decimal total = payment.Order.Fare + payment.Order.Insurance + payment.Order.Vat;
            decimal paid = await _context.Payments
                .Where(p => p.OrderId == payment.OrderId && p.Id != id)
                .SumAsync(p => p.Amount);
            decimal remaining = total - paid;
            if (dto.Amount.Value > remaining)
                throw new BadRequestException("مبلغ پرداخت بیشتر از مبلغ باقی‌مانده سفارش است.");
            payment.Amount = dto.Amount.Value;
        }
        if (!string.IsNullOrWhiteSpace(dto.TransactionId))
            payment.TransactionId = dto.TransactionId;

        await _context.SaveChangesAsync();

        return MapToDto(payment);
    }

    public async Task<bool> DeleteAsync(long id, long currentUserId)
    {
        _logger.LogInformation("در حال حذف پرداخت {PaymentId} توسط کاربر {UserId}", id, currentUserId);

        var payment = await _context.Payments
            .Include(p => p.Order).ThenInclude(o => o.Organization)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (payment == null)
        {
            _logger.LogWarning("پرداخت با شناسه {PaymentId} یافت نشد", id);
            return false;
        }

        var currentUser = await _context.Persons.FindAsync(currentUserId)
            ?? throw new NotFoundException("کاربر جاری یافت نشد.");

        await OrderAccessGuard.EnsureUserCanAccessOrderAsync(payment.Order, currentUser, _context);

        _context.Payments.Remove(payment);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<decimal> GetRemainingAmountAsync(long orderId, long currentUserId)
    {
        _logger.LogInformation("محاسبه مبلغ باقی‌مانده پرداخت برای سفارش {OrderId} توسط کاربر {UserId}", orderId, currentUserId);

        var order = await _context.Orders
            .Include(o => o.Organization)
            .FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new NotFoundException("سفارش یافت نشد.");

        var currentUser = await _context.Persons.FindAsync(currentUserId)
            ?? throw new NotFoundException("کاربر جاری یافت نشد.");

        await OrderAccessGuard.EnsureUserCanAccessOrderAsync(order, currentUser, _context);

        decimal total = order.Fare + order.Insurance + order.Vat;
        decimal paid = await _context.Payments
            .Where(p => p.OrderId == orderId)
            .SumAsync(p => p.Amount);

        decimal remaining = total - paid;
        _logger.LogInformation("Total: {Total} | Paid: {Paid} | Remaining: {Remaining}", total, paid, remaining);

        return remaining > 0 ? remaining : 0;
    }

    private static PaymentDto MapToDto(Payment payment) => new PaymentDto
    {
        Id = payment.Id,
        OrderId = payment.OrderId,
        PaymentType = payment.PaymentMethod,
        Amount = payment.Amount,
        PaymentDate = payment.PaymentDate,
        TransactionId = payment.TransactionId
    };
}
