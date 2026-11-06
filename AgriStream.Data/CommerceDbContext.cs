using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgriStreamIOT.Data
{
    public class Order
    {
        [Key]
        public Guid OrderId { get; set; }

        [Required]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        public string ProductName { get; set; } = string.Empty;

        public int Quantity { get; set; }

        [Precision(18, 2)]
        public decimal UnitPrice { get; set; }

        [Precision(18, 2)]
        public decimal TotalAmount { get; set; }

        public DateTime OrderDate { get; set; }

        public DateTime? ProcessedAt { get; set; }

        public string Status { get; set; } = "Pending";
    }

    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Password { get; set; } = string.Empty;

        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public ICollection<Field> Fields { get; set; } = new List<Field>();
    }

    public class Role
    {
        [Key]
        public int RoleId { get; set; }

        [Required]
        [MaxLength(50)]
        public string RoleName { get; set; } = string.Empty;

        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }

    public class UserRole
    {
        [Key]
        public int UserRoleId { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int RoleId { get; set; }
        public Role Role { get; set; } = null!;
    }

    public class Field
    {
        [Key]
        public int FieldId { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Location { get; set; } = string.Empty;

        public int OwnerId { get; set; }
        public User Owner { get; set; } = null!;

        public ICollection<DeviceStatus> Devices { get; set; } = new List<DeviceStatus>();
    }

    public class DeviceStatus
    {
        [Key]
        public int DeviceStatusId { get; set; }

        [Required]
        [MaxLength(100)]
        public string DeviceName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Status { get; set; } = "Healthy";

        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        public int FieldId { get; set; }
        public Field Field { get; set; } = null!;
    }

    public class AgriStreamDbContext : DbContext
    {
        public AgriStreamDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<UserRole> UserRoles { get; set; } = null!;
        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<Field> Fields { get; set; } = null!;
        public DbSet<DeviceStatus> DeviceStatuses { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Role>().HasData(
                new Role { RoleId = 1, RoleName = "Farmer" },
                new Role { RoleId = 2, RoleName = "Agronomist" },
                new Role { RoleId = 3, RoleName = "Admin" });

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId);

            modelBuilder.Entity<Field>()
                .HasOne(f => f.Owner)
                .WithMany(u => u.Fields)
                .HasForeignKey(f => f.OwnerId);

            modelBuilder.Entity<DeviceStatus>()
                .HasOne(ds => ds.Field)
                .WithMany(f => f.Devices)
                .HasForeignKey(ds => ds.FieldId);
        }
    }

    public class CommerceDbContext : AgriStreamDbContext
    {
        public CommerceDbContext(DbContextOptions<CommerceDbContext> options) : base(options)
        {
        }

        public DbSet<Order> Orders { get; set; } = null!;
    }
}