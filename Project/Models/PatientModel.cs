namespace Project.Models
{
    public class PatientModel
    {
        public string HN { get; set; } = string.Empty;
        public string? CitizenID { get; set; }
        public int? TitleID { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime? BirthDate { get; set; }
        public decimal? Weight { get; set; }
        public decimal? Height { get; set; }
        public int? RightID { get; set; }
        public string? Phone { get; set; }
        public string? PatientType { get; set; }
        public int? TambonID { get; set; }
    }
}
