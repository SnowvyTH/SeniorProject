using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project.Models.Entity
{
    [Table("EvaluationDetail")]
    public class EvaluationDetail
    {
        [Key]
        public int EvaDetailID { get; set; }

        public int? Score { get; set; }

        [ForeignKey("Evaluation")]
        public int? EvaID { get; set; }

        [ForeignKey("Question")]
        public int? QuestionID { get; set; }

        public virtual Evaluation? Evaluation { get; set; }
        public virtual Question? Question { get; set; }
    }
}
