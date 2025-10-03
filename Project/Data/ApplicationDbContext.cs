using Microsoft.EntityFrameworkCore;
using Project.Models.Entity;

namespace Project.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Role> Role { get; set; }
        public DbSet<Title> Title { get; set; }
        public DbSet<Treatment> Treatment { get; set; }
        public DbSet<Form> Form { get; set; }
        public DbSet<User> User { get; set; }
        public DbSet<Log> Log { get; set; }
        public DbSet<Patient> Patient { get; set; }
        public DbSet<Province> Province { get; set; }
        public DbSet<Amphur> Amphur { get; set; }
        public DbSet<District> District { get; set; }
        public DbSet<Question> Question { get; set; }
        public DbSet<Option> Option { get; set; }
        public DbSet<Evaluation> Evaluation { get; set; }
        public DbSet<EvaluationDetail> EvaluationDetail { get; set; }
        public DbSet<Interpreter> Interpreter { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Patient>()
                .HasOne(p => p.Province)
                .WithMany(prov => prov.Patients)
                .HasForeignKey(p => p.PROVINCE_ID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Patient>()
                .HasOne(p => p.Amphur)
                .WithMany(a => a.Patients)
                .HasForeignKey(p => p.AMPHUR_ID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Patient>()
                .HasOne(p => p.District)
                .WithMany(d => d.Patients)
                .HasForeignKey(p => p.DISTRICT_ID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Amphur>()
                .HasOne(a => a.Province)
                .WithMany(p => p.Amphurs)
                .HasForeignKey(a => a.PROVINCE_ID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<District>()
                .HasOne(d => d.Amphur)
                .WithMany(a => a.Districts)
                .HasForeignKey(d => d.AMPHUR_ID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<District>()
                .HasOne(d => d.Province)
                .WithMany(p => p.Districts)
                .HasForeignKey(d => d.PROVINCE_ID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Title)
                .WithMany(t => t.Users)
                .HasForeignKey(u => u.TitleID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Patient>()
                .HasOne(p => p.Title)
                .WithMany(t => t.Patients)
                .HasForeignKey(p => p.TitleID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Patient>()
                .HasOne(p => p.Treatment)
                .WithMany(t => t.Patients)
                .HasForeignKey(p => p.TreatmentID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Log>()
                .HasOne(l => l.User)
                .WithMany(u => u.Logs)
                .HasForeignKey(l => l.UserID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Evaluation>()
                .HasOne(e => e.User)
                .WithMany(u => u.Evaluations)
                .HasForeignKey(e => e.UserID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Evaluation>()
                .HasOne(e => e.Patient)
                .WithMany(p => p.Evaluations)
                .HasForeignKey(e => e.HN)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Evaluation>()
                .HasOne(e => e.Form)
                .WithMany(f => f.Evaluations)
                .HasForeignKey(e => e.FormID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Question>()
                .HasOne(q => q.Form)
                .WithMany(f => f.Questions)
                .HasForeignKey(q => q.FormID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Option>()
                .HasOne(o => o.Question)
                .WithMany(q => q.Options)
                .HasForeignKey(o => o.QuestionID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EvaluationDetail>()
                .HasOne(ed => ed.Evaluation)
                .WithMany(e => e.EvaluationDetails)
                .HasForeignKey(ed => ed.EvaID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EvaluationDetail>()
                .HasOne(ed => ed.Question)
                .WithMany(q => q.EvaluationDetails)
                .HasForeignKey(ed => ed.QuestionID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Interpreter>()
                .HasOne(i => i.Form)
                .WithMany(f => f.Interpreters)
                .HasForeignKey(i => i.FormID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();
        }
    }
}
