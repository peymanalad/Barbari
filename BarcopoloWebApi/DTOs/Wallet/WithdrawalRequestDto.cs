using System;

namespace BarcopoloWebApi.DTOs.Withdrawal
{
    public class WithdrawalRequestDto
    {
        public long Id { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewedBy { get; set; }
    }
}