using System.Globalization;

namespace BarcopoloWebApi.DTOs.Driver
{
    public class DriverDto
    {
        public long Id { get; set; }

        public string SmartCardCode { get; set; }
        public string IdentificationNumber { get; set; }
        public string LicenseNumber { get; set; }
        public DateTime LicenseExpiryDate { get; set; }

        public bool HasViolations { get; set; }

        public string FullName { get; set; }
        public string PhoneNumber { get; set; }

        public string LicenseExpiryDateShamsi =>
            LicenseExpiryDate.ToString("yyyy/MM/dd", new CultureInfo("fa-IR"));
    }
}