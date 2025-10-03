using System.ComponentModel.DataAnnotations;

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
        
        [DataType(DataType.Date)]
        [Display(Name = "วันที่บันทึกเวชระเบียน")]
        [RecordDateValidation]
        public DateTime? RecordDate { get; set; }
        
        public decimal? Weight { get; set; }
        public decimal? Height { get; set; }
        public int? RightID { get; set; }
        public string? Phone { get; set; }
        public string? PatientType { get; set; }
        public int? TambonID { get; set; }
    }
    
    public class RecordDateValidationAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value is DateTime date)
            {
                return date.Date <= DateTime.Today;
            }
            return true; 
        }
        
        public override string FormatErrorMessage(string name)
        {
            return "วันที่บันทึกเวชระเบียนไม่สามารถเกินวันปัจจุบันได้";
        }
    }
}
