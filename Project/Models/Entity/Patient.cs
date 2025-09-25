using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project.Models.Entity
{
    [Table("Patient")]
    public class Patient
    {
        [Key]
        [StringLength(20)]
        public string HN { get; set; } = string.Empty;

        [StringLength(50)]
        public string? FirstName { get; set; }

        [StringLength(50)]
        public string? LastName { get; set; }

        public DateTime? BirthDate { get; set; }

        [StringLength(13)]
        public string? CitizenID { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? Height { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? Weight { get; set; }

        [StringLength(10)]
        public string? Tel { get; set; }

        [StringLength(4)]
        public string? PatientType { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime? RecordDate { get; set; }

        [ForeignKey("Title")]
        public int? TitleID { get; set; }

        [ForeignKey("Treatment")]
        public int? TreatmentID { get; set; }

        [ForeignKey("Province")]
        public int? PROVINCE_ID { get; set; }

        [ForeignKey("Amphur")]
        public int? AMPHUR_ID { get; set; }

        [ForeignKey("District")]
        public int? DISTRICT_ID { get; set; }
        

        public virtual Title? Title { get; set; }
        public virtual Treatment? Treatment { get; set; }
        public virtual Province? Province { get; set; }
        public virtual Amphur? Amphur { get; set; }
        public virtual District? District { get; set; }
        public virtual ICollection<Evaluation> Evaluations { get; set; } = new List<Evaluation>();
    }
}
