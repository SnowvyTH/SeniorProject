using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project.Models.Entity
{
    [Table("Role")]
    public class Role
    {
        [Key]
        public int RoleID { get; set; }

        [StringLength(20)]
        public string? RoleName { get; set; }

        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}
