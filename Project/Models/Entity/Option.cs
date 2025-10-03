using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project.Models.Entity
{
    [Table("Option")]
    public class Option
    {
        [Key]
        public int OptionID { get; set; }

        [StringLength(500)]
        public string? OptionText { get; set; }

        public int? Value { get; set; }

        [ForeignKey("Question")]
        public int? QuestionID { get; set; }

        public virtual Question? Question { get; set; }
    }
}
