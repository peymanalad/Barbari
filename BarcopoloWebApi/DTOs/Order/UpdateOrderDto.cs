﻿using System.ComponentModel.DataAnnotations;

public class UpdateOrderDto
{
    [Required]
    public string? SenderName { get; set; }

    [Required]
    [RegularExpression(@"^09\d{9}$", ErrorMessage = "شماره موبایل فرستنده نامعتبر است")]
    public string? SenderPhone { get; set; }

    [Required]
    public string? ReceiverName { get; set; }

    [Required]
    [RegularExpression(@"^09\d{9}$", ErrorMessage = "شماره موبایل گیرنده نامعتبر است")]
    public string? ReceiverPhone { get; set; }

    // --- Details ---
    public string? Details { get; set; }
    public DateTime? LoadingTime { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? DeclaredValue { get; set; } = 0;
    public bool? IsInsuranceRequested { get; set; } = false;
}