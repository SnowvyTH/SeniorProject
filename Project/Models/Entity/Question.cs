using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project.Models.Entity
{
    [Table("Question")]
    public class Question
    {
        [Key]
        public int QuestionID { get; set; }

        [StringLength(500)]
        public string? QuestionText { get; set; }

        [StringLength(75)]
        public string? Section { get; set; }

        [ForeignKey("Form")]
        public int? FormID { get; set; }
        public virtual Form? Form { get; set; }
        public virtual ICollection<Option> Options { get; set; } = new List<Option>();
        public virtual ICollection<EvaluationDetail> EvaluationDetails { get; set; } = new List<EvaluationDetail>();
    }
}
