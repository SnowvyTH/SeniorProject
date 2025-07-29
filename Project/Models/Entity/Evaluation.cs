using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project.Models.Entity
{
    [Table("Evaluation")]
    public class Evaluation
    {
        [Key]
        public int EvaID { get; set; }

        [StringLength(150)]
        public string? Result { get; set; }
        public DateTime RecordDate { get; set; } = DateTime.Now;

        [ForeignKey("User")]
        public int? UserID { get; set; }

        [ForeignKey("Patient")]
        public string? HN { get; set; }

        [ForeignKey("Form")]
        public int? FormID { get; set; }

        public virtual User? User { get; set; }
        public virtual Patient? Patient { get; set; }
        public virtual Form? Form { get; set; }
        public virtual ICollection<EvaluationDetail> EvaluationDetails { get; set; } = new List<EvaluationDetail>();
    }
}
