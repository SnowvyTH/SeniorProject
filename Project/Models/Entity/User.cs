using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project.Models.Entity
{
    [Table("User")]
    public class User
    {
        [Key]
        public int UserID { get; set; }

        [Required]
        [StringLength(64)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(256)]
        public string PasswordHash { get; set; } = string.Empty;

        [StringLength(50)]
        public string? FirstName { get; set; }

        [StringLength(50)]
        public string? LastName { get; set; }

        public bool IsActive { get; set; }
        public string? Picture { get; set; }

        [ForeignKey("Role")]
        public int? RoleID { get; set; }

        [ForeignKey("Title")]
        public int? TitleID { get; set; }
        public virtual Role? Role { get; set; }
        public virtual Title? Title { get; set; }
        public virtual ICollection<Log> Logs { get; set; } = new List<Log>();
        public virtual ICollection<Evaluation> Evaluations { get; set; } = new List<Evaluation>();
    }
}
