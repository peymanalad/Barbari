namespace BarcopoloWebApi.Enums
{
    public enum OrderStatus
    {
        Pending = 0,      // در انتظار بررسی/تخصیص
        Assigned = 1,     // تخصیص یافته (به راننده/خودرو)
        Loading = 2,      // در حال بارگیری
        InProgress = 3,   // در حال حمل
        Unloading = 4,    // در حال تخلیه
        Delivered = 5,    // تحویل شده
        Cancelled = 6,    // لغو شده
        Returned = 7      // مرجوع شده
    }
}
