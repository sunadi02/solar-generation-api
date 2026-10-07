using Microsoft.EntityFrameworkCore;
using SolarGenerationApi.Models;

namespace SolarGenerationApi.Data;

public static class DbSeeder
{
    public static void Seed(SolarGenerationDbContext db)
    {
        if (db.Provinces.Any()) return; // දැනටමත් data තියෙනවා නම් ආයෙත් දාන්නේ නැහැ

        var rnd = new Random(42);

        // 1. Provinces සහ Districts (9 + 25)
        var geo = new (string Province, string[] Districts)[]
        {
            ("Western", new[] { "Colombo", "Gampaha", "Kalutara" }),
            ("Central", new[] { "Kandy", "Matale", "Nuwara Eliya" }),
            ("Southern", new[] { "Galle", "Matara", "Hambantota" }),
            ("Northern", new[] { "Jaffna", "Kilinochchi", "Mannar", "Vavuniya", "Mullaitivu" }),
            ("Eastern", new[] { "Batticaloa", "Ampara", "Trincomalee" }),
            ("North Western", new[] { "Kurunegala", "Puttalam" }),
            ("North Central", new[] { "Anuradhapura", "Polonnaruwa" }),
            ("Uva", new[] { "Badulla", "Monaragala" }),
            ("Sabaragamuwa", new[] { "Ratnapura", "Kegalle" })
        };

        foreach (var (pName, dNames) in geo)
        {
            var province = new Province { Name = pName };
            foreach (var d in dNames)
                province.Districts.Add(new District { Name = d });
            db.Provinces.Add(province);
        }
        db.SaveChanges();

        // 2. Grid substations (district එකකට එකක්, ලොකු districts 5කට දෙකක්)
        var bigDistricts = new[] { "Colombo", "Gampaha", "Kandy", "Galle", "Kurunegala" };
        var substations = new List<GridSubstation>();
        int code = 1;
        foreach (var district in db.Districts.OrderBy(d => d.Id).ToList())
        {
            int count = bigDistricts.Contains(district.Name) ? 2 : 1;
            for (int k = 1; k <= count; k++)
            {
                substations.Add(new GridSubstation
                {
                    Name = $"{district.Name} Grid Substation {k}",
                    Code = $"GSS-{code++:000}",
                    DistrictId = district.Id
                });
            }
        }
        db.GridSubstations.AddRange(substations);
        db.SaveChanges();

        // 3. Users (password: demo විදිහට)
        var western = db.Provinces.First(p => p.Name == "Western");
        var colombo = db.Districts.First(d => d.Name == "Colombo");
        db.Users.AddRange(
            new User { Username = "admin", Role = "Admin", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123") },
            new User { Username = "national", Role = "National", PasswordHash = BCrypt.Net.BCrypt.HashPassword("National@123") },
            new User { Username = "western", Role = "Provincial", ProvinceId = western.Id, PasswordHash = BCrypt.Net.BCrypt.HashPassword("Western@123") },
            new User { Username = "colombo", Role = "District", DistrictId = colombo.Id, PasswordHash = BCrypt.Net.BCrypt.HashPassword("Colombo@123") }
        );
        db.SaveChanges();

        // 4. Solar installations (240)
        var firstNames = new[] { "Nimal", "Kamal", "Sunil", "Saman", "Chamari", "Nadeesha", "Ruwan", "Dilani", "Arun", "Priya", "Kumar", "Lakshmi", "Ashan", "Tharindu", "Sanduni" };
        var lastNames = new[] { "Perera", "Fernando", "Silva", "Jayasinghe", "Wickramasinghe", "Rajapaksha", "Kumar", "Selvam", "Bandara", "Gunawardena" };
        var subList = db.GridSubstations.Include(s => s.District).OrderBy(s => s.Id).ToList();
        var installations = new List<SolarInstallation>();

        for (int i = 1; i <= 240; i++)
        {
            var sub = subList[(i - 1) % subList.Count];
            var meter = $"MTR-{i:000000}";
            installations.Add(new SolarInstallation
            {
                MeterIdentifier = meter,
                OwnerName = $"{firstNames[rnd.Next(firstNames.Length)]} {lastNames[rnd.Next(lastNames.Length)]}",
                Address = $"{rnd.Next(1, 200)}, Main Street, {sub.District!.Name}",
                CapacityKw = Math.Round(3 + rnd.NextDouble() * 12, 1), // 3 - 15 kW
                InstalledOn = new DateTime(2019, 1, 1).AddDays(rnd.Next(0, 2500)),
                UpdatedAt = DateTime.UtcNow,
                DeviceSecretHash = BCrypt.Net.BCrypt.HashPassword($"secret-{meter}"),
                GridSubstationId = sub.Id
            });
        }
        db.SolarInstallations.AddRange(installations);
        db.SaveChanges();

        // 5. පසුගිය දවස් 7 ක readings (විනාඩි 30 ට වරක්)
        var end = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day,
                               DateTime.UtcNow.Hour, DateTime.UtcNow.Minute < 30 ? 0 : 30, 0, DateTimeKind.Utc);
        var start = end.AddDays(-7);
        var dayCloud = new Dictionary<int, double>();

        foreach (var inst in installations)
        {
            var readings = new List<GenerationReading>();
            double energy = 0;

            for (var t = start; t <= end; t = t.AddMinutes(30))
            {
                var local = t.AddHours(5.5); // Sri Lanka වේලාව
                double hour = local.Hour + local.Minute / 60.0;
                double power = 0;

                if (hour >= 6 && hour < 18)
                {
                    int dayKey = inst.Id * 1000 + (int)(t - start).TotalDays;
                    if (!dayCloud.TryGetValue(dayKey, out var cloud))
                    {
                        cloud = 0.5 + rnd.NextDouble() * 0.5; // දවසේ වලාකුළු බලපෑම
                        dayCloud[dayKey] = cloud;
                    }
                    power = Math.Sin(Math.PI * (hour - 6) / 12) * inst.CapacityKw * cloud
                            * (0.95 + rnd.NextDouble() * 0.05);
                    power = Math.Round(Math.Max(power, 0), 3);
                }

                energy += power * 0.5; // kW x පැය 0.5
                readings.Add(new GenerationReading
                {
                    SolarInstallationId = inst.Id,
                    Timestamp = t,
                    InstantaneousPowerKw = power,
                    CumulativeEnergyKwh = Math.Round(energy, 3),
                    Voltage = Math.Round(229 + rnd.NextDouble() * 9, 1)
                });
            }

            db.GenerationReadings.AddRange(readings);
            db.SaveChanges();
            db.ChangeTracker.Clear();
        }
    }
}