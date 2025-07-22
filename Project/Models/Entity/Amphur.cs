using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project.Models.Entity
{
    [Table("Amphur")]
    public class Amphur
    {
        [Key]
        public int AMPHUR_ID { get; set; }

        [StringLength(4)]
        public string? AMPHUR_CODE { get; set; }

        [StringLength(150)]
        public string? AMPHUR_NAME { get; set; }

        public int? GEO_ID { get; set; }

        [ForeignKey("Province")]
        public int? PROVINCE_ID { get; set; }

        public virtual Province? Province { get; set; }
        public virtual ICollection<District> Districts { get; set; } = new List<District>();
        public virtual ICollection<Patient> Patients { get; set; } = new List<Patient>();
    }
}
