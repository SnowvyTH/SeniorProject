using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project.Models.Entity
{
    [Table("Treatment")]
    public class Treatment
    {
        [Key]
        public int TreatmentID { get; set; }

        [StringLength(30)]
        public string? TreatmentName { get; set; }

        public virtual ICollection<Patient> Patients { get; set; } = new List<Patient>();
    }
}
