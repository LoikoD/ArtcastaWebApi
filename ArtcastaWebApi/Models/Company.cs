namespace ArtcastaWebApi.Models
{
    public class Company
    {
        public int CompanyId { get; set; }

        public string CompanyNameShort { get; set; } = string.Empty;

        public string CompanyNameFull { get; set; } = string.Empty;

        public string CompanyAddress { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Ogrn { get; set; } = string.Empty;

        public string Inn { get; set; } = string.Empty;

        public string Director { get; set; } = string.Empty;

        public string DirectorPosition { get; set; } = string.Empty;

        public string DirectorInfoDative { get; set; } = string.Empty;
    }
}
