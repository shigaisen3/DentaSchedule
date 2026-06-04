using DentaSchedule.DAL.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DentaSchedule.DAL.Data;

public static class SeedData
{
    public const string AdminRole = "Admin";
    public const string AssistantRole = "Assistant";

    public const string AdminEmail = "admin@dentaschedule.com";

    /// <summary>
    /// Development-only fallback used when <c>Seed:AdminPassword</c> is not configured.
    /// In non-Development environments the caller must supply a real password.
    /// </summary>
    public const string DevDefaultAdminPassword = "Admin@123";

    // Fixed GUIDs so re-seeding is idempotent
    public static readonly Guid TestClinicId = new("11111111-0000-0000-0000-000000000001");
    public static readonly Guid TestDoctorId = new("22222222-0000-0000-0000-000000000001");

    private static readonly DayOfWeek[] Workdays =
    {
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
        DayOfWeek.Thursday, DayOfWeek.Friday
    };

    // 2 doctors per clinic (indexed by clinic slot 0-9, 2 doctors each)
    private static readonly (string FullName, string Specialization, string Bio)[] ClinicDoctors =
    {
        // Clinic 1 – Denta Bright
        ("Dr. Mihai Ionescu",    "Stomatologie Generală",  "Experiență de 8 ani în stomatologie generală și estetică."),
        ("Dr. Elena Dumitrescu", "Ortodonție",             "Specialist în aparate dentare și corecții ortodontice."),
        // Clinic 2 – SmilePro
        ("Dr. Alexandru Popa",   "Implantologie",          "Specialist în implanturi dentare cu peste 500 de intervenții."),
        ("Dr. Ioana Stancu",     "Parodontologie",         "Tratamente avansate ale gingiei și țesuturilor de susținere."),
        // Clinic 3 – DentaCare Plus
        ("Dr. Cristian Marin",   "Endodonție",             "Specialist în tratamente de canal și restaurări complexe."),
        ("Dr. Laura Georgescu",  "Stomatologie Pediatrică","Experiență de 6 ani în stomatologia copiilor."),
        // Clinic 4 – OralHealth
        ("Dr. Radu Florescu",    "Chirurgie Orală",        "Chirurg oral cu specializare în extractii și implanturi."),
        ("Dr. Simona Diaconu",   "Protetica Dentară",      "Specialist în coroane, punți și proteze dentare."),
        // Clinic 5 – WhiteSmile
        ("Dr. Bogdan Niculescu", "Stomatologie Estetică",  "Albiri dentare, fațete și restaurări estetice."),
        ("Dr. Andreea Constantin","Ortodonție",            "Aparate dentare fixe și mobile, aliniere invizibilă."),
        // Clinic 6 – DentaPlus
        ("Dr. Vlad Petrescu",    "Implantologie",          "Implanturi unitare și restaurări full-arch."),
        ("Dr. Diana Moldovan",   "Endodonție",             "Tratamente de canal cu microscopie dentară."),
        // Clinic 7 – PerfectSmile
        ("Dr. Claudiu Ene",      "Chirurgie Orală",        "Intervenții chirurgicale orale și parodontale."),
        ("Dr. Roxana Barbu",     "Stomatologie Generală",  "Consultații complete și tratamente preventive."),
        // Clinic 8 – DentalArt
        ("Dr. Marius Stoica",    "Stomatologie Estetică",  "Transformări zâmbet complet, fațete ceramice."),
        ("Dr. Alina Voicu",      "Parodontologie",         "Tratamente non-chirurgicale și chirurgicale ale gingiei."),
        // Clinic 9 – BrightDent
        ("Dr. Silviu Oprea",     "Protetica Dentară",      "Lucrări protetice fixe și mobile de înaltă calitate."),
        ("Dr. Gabriela Toma",    "Stomatologie Pediatrică","Tratamente blânde și preventive pentru copii."),
        // Clinic 10 – SmileFirst
        ("Dr. Ionuț Dragomir",   "Ortodonție",             "Specialist ortodont, aparate metalice și ceramice."),
        ("Dr. Oana Nistor",      "Stomatologie Generală",  "Experiență largă în profilaxie și restaurări dentare."),
    };

    private static readonly (string Name, string Address, string Phone, string Email)[] TestClinics =
    {
        ("Denta Bright Clinic",   "Str. Florilor 5, Cluj-Napoca",      "0721 100 001", "contact@dentabright.ro"),
        ("SmilePro Dental",       "Bd. Unirii 22, Timișoara",          "0721 100 002", "contact@smilepro.ro"),
        ("DentaCare Plus",        "Str. Libertății 8, Iași",           "0721 100 003", "contact@dentacareplus.ro"),
        ("OralHealth Clinic",     "Str. Mihai Eminescu 14, Brașov",    "0721 100 004", "contact@oralhealth.ro"),
        ("WhiteSmile Dental",     "Calea Dorobanților 3, Cluj-Napoca", "0721 100 005", "contact@whitesmile.ro"),
        ("DentaPlus Center",      "Str. Republicii 17, Ploiești",      "0721 100 006", "contact@dentaplus.ro"),
        ("PerfectSmile Clinic",   "Bd. Ferdinand 9, Constanța",        "0721 100 007", "contact@perfectsmile.ro"),
        ("DentalArt Studio",      "Str. Avram Iancu 21, Oradea",       "0721 100 008", "contact@dentalart.ro"),
        ("BrightDent Clinic",     "Str. Decebal 6, Sibiu",             "0721 100 009", "contact@brightdent.ro"),
        ("SmileFirst Dental",     "Bd. Independenței 33, Craiova",     "0721 100 010", "contact@smilefirst.ro"),
    };

    public static async Task InitializeAsync(IServiceProvider serviceProvider, string adminPassword)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<AppRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
        var db = serviceProvider.GetRequiredService<DentaScheduleDbContext>();

        // Seed roles
        string[] roles = { AdminRole, AssistantRole };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new AppRole { Name = role });
            }
        }

        // Seed admin user
        var adminUser = await userManager.FindByEmailAsync(AdminEmail);
        if (adminUser == null)
        {
            adminUser = new AppUser
            {
                UserName = AdminEmail,
                Email = AdminEmail,
                DisplayName = "System Administrator",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, AdminRole);
            }
        }

        // Seed test clinic
        var clinicExists = await db.Clinics
            .IgnoreQueryFilters()
            .AnyAsync(c => c.Id == TestClinicId);

        if (!clinicExists)
        {
            db.Clinics.Add(new Clinic
            {
                Id = TestClinicId,
                Name = "DentaSmile Clinic",
                Address = "Str. Victoriei 10, București",
                Phone = "0721 000 001",
                Email = "contact@dentasmile.ro",
                IsActive = true
            });
            await db.SaveChangesAsync();
        }

        // Seed test doctor
        var doctorExists = await db.Doctors
            .IgnoreQueryFilters()
            .AnyAsync(d => d.Id == TestDoctorId);

        if (!doctorExists)
        {
            db.Doctors.Add(new Doctor
            {
                Id = TestDoctorId,
                FullName = "Dr. Andreea Popescu",
                Specialization = "Stomatologie Generală",
                Bio = "Medic stomatolog cu peste 10 ani de experiență.",
                ClinicId = TestClinicId,
                IsActive = true
            });
            await db.SaveChangesAsync();

            // Weekly schedule: Mon–Fri, 09:00–17:00, 30-minute slots
            foreach (var day in Workdays)
            {
                db.DoctorSchedules.Add(new DoctorSchedule
                {
                    Id = Guid.NewGuid(),
                    DoctorId = TestDoctorId,
                    DayOfWeek = day,
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(17, 0, 0),
                    SlotDurationMinutes = 30
                });
            }
            await db.SaveChangesAsync();
        }

        // Seed 10 test clinics + 1 assistant each
        for (var i = 0; i < TestClinics.Length; i++)
        {
            var num = i + 1;
            var clinicId = new Guid($"33333333-0000-0000-0000-{num:D12}");
            var (name, address, phone, email) = TestClinics[i];

            var clinicAlreadyExists = await db.Clinics
                .IgnoreQueryFilters()
                .AnyAsync(c => c.Id == clinicId);

            if (!clinicAlreadyExists)
            {
                db.Clinics.Add(new Clinic
                {
                    Id = clinicId,
                    Name = name,
                    Address = address,
                    Phone = phone,
                    Email = email,
                    IsActive = true
                });
                await db.SaveChangesAsync();
            }

            // Seed 2 doctors for this clinic
            for (var d = 0; d < 2; d++)
            {
                var doctorIdx = i * 2 + d;
                var doctorId = new Guid($"44444444-0000-0000-{num:D4}-{d + 1:D12}");
                var doctorAlreadyExists = await db.Doctors
                    .IgnoreQueryFilters()
                    .AnyAsync(doc => doc.Id == doctorId);

                if (!doctorAlreadyExists)
                {
                    var (fullName, specialization, bio) = ClinicDoctors[doctorIdx];
                    db.Doctors.Add(new Doctor
                    {
                        Id = doctorId,
                        FullName = fullName,
                        Specialization = specialization,
                        Bio = bio,
                        ClinicId = clinicId,
                        IsActive = true
                    });
                    await db.SaveChangesAsync();

                    // Mon–Fri, 09:00–17:00, 30-minute slots
                    foreach (var day in Workdays)
                    {
                        db.DoctorSchedules.Add(new DoctorSchedule
                        {
                            Id = Guid.NewGuid(),
                            DoctorId = doctorId,
                            DayOfWeek = day,
                            StartTime = new TimeSpan(9, 0, 0),
                            EndTime = new TimeSpan(17, 0, 0),
                            SlotDurationMinutes = 30
                        });
                    }
                    await db.SaveChangesAsync();
                }
            }

            var assistantEmail = $"assistant{num}@dentaschedule.com";
            var existingAssistant = await userManager.FindByEmailAsync(assistantEmail);
            if (existingAssistant == null)
            {
                var assistant = new AppUser
                {
                    UserName = assistantEmail,
                    Email = assistantEmail,
                    DisplayName = $"Assistant {num}",
                    ClinicId = clinicId,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(assistant, $"Assistant{num}@123");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(assistant, AssistantRole);
            }
        }
    }
}
