using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project.Models.Entity
{
    [Table("Interpreter")]
    public class Interpreter
    {
        [Key]
        public int InterID { get; set; }

        [StringLength(75)]
        public string? InterName { get; set; }

        [StringLength(500)]
        public string? InterText { get; set; }

        public int? MinScore { get; set; }

        public int? MaxScore { get; set; }

        [ForeignKey("Form")]
        public int? FormID { get; set; }

        public virtual Form? Form { get; set; }
    }
}
