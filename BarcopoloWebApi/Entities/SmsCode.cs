namespace BarcopoloWebApi.Entities
{
    public class SmsCode
    {
        public long Id { get; set; }
        public string PhoneNumber { get; set; }
        public string Code { get; set; }
        public bool Used { get; set; }
        public int RequestCount { get; set; }
        public DateTime InsertDate { get; set; }
    }
}
