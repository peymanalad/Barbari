using BarcopoloWebApi.DTOs.Feedback;

namespace BarcopoloWebApi.Services
{
    public interface IFeedbackService
    {
        Task<FeedbackDto> CreateAsync(CreateFeedbackDto dto, long currentUserId);
        Task<FeedbackDto> UpdateAsync(long id, UpdateFeedbackDto dto, long currentUserId);
        Task<bool> DeleteAsync(long id, long currentUserId);

        Task<FeedbackDto> GetByOrderIdAsync(long orderId, long currentUserId);
        Task<FeedbackDto> GetByIdAsync(long feedbackId, long currentUserId);

    }
}