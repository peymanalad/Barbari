using System.ComponentModel.DataAnnotations;
using BarcopoloWebApi.Enums;

namespace BarcopoloWebApi.DTOs.Withdrawal
{
    public class ReviewWithdrawalRequestDto
    {
        [Required]
        public WithdrawalRequestStatus Status { get; set; }

        [MaxLength(255)]
        public string? ReviewNote { get; set; }
    }
}