using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project.Models.Entity
{
    [Table("Province")]
    public class Province
    {
        [Key]
        public int PROVINCE_ID { get; set; }

        [StringLength(2)]
        public string? PROVINCE_CODE { get; set; }

        [StringLength(150)]
        public string? PROVINCE_NAME { get; set; }

        public int? GEO_ID { get; set; }

        public virtual ICollection<Amphur> Amphurs { get; set; } = new List<Amphur>();
        public virtual ICollection<District> Districts { get; set; } = new List<District>();
        public virtual ICollection<Patient> Patients { get; set; } = new List<Patient>();
    }
}
