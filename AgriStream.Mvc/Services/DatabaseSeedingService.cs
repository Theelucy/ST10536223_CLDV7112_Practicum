using AgriStreamIOT.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AgriStreamIOT.Mvc.Services
{
    public class DatabaseSeedingService
    {
        private readonly AgriStreamDbContext _context;

        public DatabaseSeedingService(AgriStreamDbContext context)
        {
            _context = context;
        }

        public async Task SeedAsync()
        {
            try
            {
                if (!await _context.Database.CanConnectAsync())
                {
                    await _context.Database.EnsureCreatedAsync();
                }
                else
                {
                    try
                    {
                        await _context.Database.MigrateAsync();
                    }
                    catch (SqlException ex) when (
                        ex.Message.Contains("already an object named") ||
                        ex.Message.Contains("There is already an object named") ||
                        ex.Message.Contains("table already exists"))
                    {
                        // Database already contains the expected tables; continue with seeding.
                    }
                }

                // Seed Roles if not already seeded
                if (!await _context.Roles.AnyAsync())
                {
                    _context.Roles.AddRange(
                        new Role { RoleName = "Farmer" },
                        new Role { RoleName = "Agronomist" },
                        new Role { RoleName = "Admin" }
                    );
                    await _context.SaveChangesAsync();
                }

                // Seed Users if not already seeded
                if (!await _context.Users.AnyAsync())
                {
                    var farmerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Farmer");
                    
                    var user = new User
                    {
                        Username = "farmer1",
                        Password = "password123",
                        FirstName = "John",
                        LastName = "Doe",
                        Email = "john@agristream.com"
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    // Assign role
                    if (farmerRole != null)
                    {
                        _context.UserRoles.Add(new UserRole
                        {
                            UserId = user.UserId,
                            RoleId = farmerRole.RoleId
                        });
                        await _context.SaveChangesAsync();
                    }
                }

                // Seed Fields if not already seeded
                if (!await _context.Fields.AnyAsync())
                {
                    var owner = await _context.Users.FirstAsync();

                    var fields = new List<Field>
                    {
                        new Field
                        {
                            Name = "North Pasture",
                            Location = "Farm Area A",
                            OwnerId = owner.UserId
                        },
                        new Field
                        {
                            Name = "South Vineyard",
                            Location = "Farm Area B",
                            OwnerId = owner.UserId
                        }
                    };

                    _context.Fields.AddRange(fields);
                    await _context.SaveChangesAsync();
                }

                // Seed Devices if not already seeded
                if (!await _context.DeviceStatuses.AnyAsync())
                {
                    var fields = await _context.Fields.ToListAsync();

                    foreach (var field in fields)
                    {
                        _context.DeviceStatuses.AddRange(
                            new DeviceStatus
                            {
                                DeviceName = $"soil-sensor-{field.FieldId}-01",
                                Status = "Healthy",
                                Temperature = 22.5,
                                Humidity = 65.0,
                                LastUpdated = DateTime.UtcNow,
                                FieldId = field.FieldId
                            },
                            new DeviceStatus
                            {
                                DeviceName = $"moisture-meter-{field.FieldId}-01",
                                Status = "Healthy",
                                Temperature = 21.0,
                                Humidity = 70.5,
                                LastUpdated = DateTime.UtcNow.AddMinutes(-5),
                                FieldId = field.FieldId
                            }
                        );
                    }

                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database seeding error: {ex.Message}");
                throw;
            }
        }
    }
}
