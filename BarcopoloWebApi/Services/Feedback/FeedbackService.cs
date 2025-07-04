﻿using BarcopoloWebApi.Data;
using BarcopoloWebApi.DTOs.Feedback;
using BarcopoloWebApi.Entities;

using BarcopoloWebApi.Services;
using Microsoft.EntityFrameworkCore;

public class FeedbackService : IFeedbackService
{
    private readonly DataBaseContext _context;
    private readonly ILogger<FeedbackService> _logger;

    public FeedbackService(DataBaseContext context, ILogger<FeedbackService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<FeedbackDto> CreateAsync(CreateFeedbackDto dto, long currentUserId)
    {
        _logger.LogInformation("Creating feedback for OrderId {OrderId} by user {UserId}", dto.OrderId, currentUserId);

        var order = await _context.Orders
            .Include(o => o.Organization)
            .Include(o => o.Owner)
            .FirstOrDefaultAsync(o => o.Id == dto.OrderId)
            ?? throw new NotFoundException("سفارش مورد نظر یافت نشد.");

        if (order.OwnerId != currentUserId)
            throw new UnauthorizedAccessAppException("شما فقط مجاز به ثبت بازخورد برای سفارش خودتان هستید.");

        bool alreadyExists = await _context.Feedbacks.AnyAsync(f => f.OrderId == dto.OrderId);
        if (alreadyExists)
            throw new InvalidOperationException("برای این سفارش قبلاً بازخورد ثبت شده است.");

        var feedback = new Feedback
        {
            OrderId = dto.OrderId,
            Rating = dto.Rating,
            Comment = dto.Comment ?? ""
        };

        _context.Feedbacks.Add(feedback);
        await _context.SaveChangesAsync();

        return MapToDto(feedback, order.Owner.GetFullName());
    }

    public async Task<FeedbackDto> GetByOrderIdAsync(long orderId, long currentUserId)
    {
        _logger.LogInformation("Retrieving feedback for OrderId {OrderId} by user {UserId}", orderId, currentUserId);

        var order = await _context.Orders
            .Include(o => o.Organization)
            .Include(o => o.Owner)
            .FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new NotFoundException("سفارش یافت نشد.");

        var currentUser = await _context.Persons.FindAsync(currentUserId)
            ?? throw new NotFoundException("کاربر جاری یافت نشد.");

        await OrderAccessGuard.EnsureUserCanAccessOrderAsync(order, currentUser, _context);

        var feedback = await _context.Feedbacks.FirstOrDefaultAsync(f => f.OrderId == orderId)
            ?? throw new NotFoundException("بازخوردی برای این سفارش یافت نشد.");

        return MapToDto(feedback, order.Owner.GetFullName());
    }

    public async Task<FeedbackDto> UpdateAsync(long id, UpdateFeedbackDto dto, long currentUserId)
    {
        _logger.LogInformation("Updating feedback {FeedbackId} by user {UserId}", id, currentUserId);

        var feedback = await _context.Feedbacks
            .Include(f => f.Order).ThenInclude(o => o.Owner)
            .FirstOrDefaultAsync(f => f.Id == id)
            ?? throw new NotFoundException("بازخورد یافت نشد.");

        if (feedback.Order.OwnerId != currentUserId)
            throw new UnauthorizedAccessAppException("فقط مالک سفارش می‌تواند بازخورد را ویرایش کند.");

        if (dto.Rating.HasValue)
            feedback.Rating = dto.Rating.Value;

        if (!string.IsNullOrWhiteSpace(dto.Comment))
            feedback.Comment = dto.Comment;

        await _context.SaveChangesAsync();

        return MapToDto(feedback, feedback.Order.Owner.GetFullName());
    }

    public async Task<bool> DeleteAsync(long id, long currentUserId)
    {
        _logger.LogInformation("Deleting feedback {FeedbackId} by user {UserId}", id, currentUserId);

        var feedback = await _context.Feedbacks
            .Include(f => f.Order).ThenInclude(o => o.Owner)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (feedback == null)
        {
            _logger.LogWarning("Feedback not found");
            return false;
        }

        if (feedback.Order.OwnerId != currentUserId &&
            !(await _context.Persons.AnyAsync(p => p.Id == currentUserId && (p.IsAdminOrSuperAdmin()))))
            throw new UnauthorizedAccessAppException("شما مجاز به حذف این بازخورد نیستید.");

        _context.Feedbacks.Remove(feedback);
        await _context.SaveChangesAsync();

        return true;
    }

    private static FeedbackDto MapToDto(Feedback f, string fullName) => new FeedbackDto
    {
        Id = f.Id,
        OrderId = f.OrderId,
        Rating = f.Rating,
        Comment = f.Comment,
        SubmittedAt = f.CreatedAt,
        CustomerFullName = fullName
    };


}
