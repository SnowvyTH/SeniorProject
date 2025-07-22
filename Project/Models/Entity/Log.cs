using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project.Models.Entity
{
    [Table("Log")]
    public class Log
    {
        [Key]
        public int LogID { get; set; }

        public DateTime? LogDateTime { get; set; }

        [ForeignKey("User")]
        public int? UserID { get; set; }

        public virtual User? User { get; set; }
    }
}
