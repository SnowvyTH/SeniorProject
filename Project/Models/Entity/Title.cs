using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project.Models.Entity
{
    [Table("Title")]
    public class Title
    {
        [Key]
        public int TitleID { get; set; }

        [StringLength(20)]
        public string? TitleName { get; set; }

        public virtual ICollection<User> Users { get; set; } = new List<User>();
        public virtual ICollection<Patient> Patients { get; set; } = new List<Patient>();
    }
}
