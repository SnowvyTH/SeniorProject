using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project.Models.Entity
{
    [Table("Form")]
    public class Form
    {
        [Key]
        public int FormID { get; set; }
        [StringLength(60)]
        public string? FormName { get; set; }
        public int FormType { get; set; }
        [StringLength(20)]
        public string? Scoring { get; set; }

        public virtual ICollection<Evaluation> Evaluations { get; set; } = new List<Evaluation>();
        public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
        public virtual ICollection<Interpreter> Interpreters { get; set; } = new List<Interpreter>();
    }
}
