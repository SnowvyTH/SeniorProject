using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project.Models.Entity
{
    [Table("District")]
    public class District
    {
        [Key]
        public int DISTRICT_ID { get; set; }

        [StringLength(6)]
        public string? DISTRICT_CODE { get; set; }

        [StringLength(150)]
        public string? DISTRICT_NAME { get; set; }

        public int? GEO_ID { get; set; }

        [ForeignKey("Amphur")]
        public int? AMPHUR_ID { get; set; }

        [ForeignKey("Province")]
        public int? PROVINCE_ID { get; set; }

        public virtual Amphur? Amphur { get; set; }
        public virtual Province? Province { get; set; }
        public virtual ICollection<Patient> Patients { get; set; } = new List<Patient>();
    }
}
