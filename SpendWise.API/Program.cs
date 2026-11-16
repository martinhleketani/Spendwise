
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SpendWise.API.Data;
using SpendWise.API.Models;
using SpendWise.API.Services;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);


// =========================================================
// DATABASE CONNECTION
// =========================================================

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString)));


// =========================================================
// ADMIN PASSWORD HASHER
// =========================================================

builder.Services.AddScoped<
    IPasswordHasher<AdminUser>,
    PasswordHasher<AdminUser>>();


// =========================================================
// PASSWORD RESET SERVICE
// =========================================================

builder.Services.AddScoped<PasswordResetService>();


// =========================================================
// WALLET SERVICES
// =========================================================

builder.Services.AddScoped<WalletService>();

builder.Services.AddScoped<WalletPaymentService>();


// =========================================================
// DOUBLE-ENTRY LEDGER SERVICE
// =========================================================

builder.Services.AddScoped<LedgerService>();


// =========================================================
// EMAIL SERVICE
// =========================================================

builder.Services.AddScoped<IEmailService, EmailService>();


// =========================================================
// AUDIT LOG SERVICE
// =========================================================

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IAuditLogService, AuditLogService>();


// =========================================================
// JWT SIGNING KEY VALIDATION
// =========================================================

var jwtKey = builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "JWT signing key was not found.");
}


// =========================================================
// CUSTOMER JWT + ADMIN COOKIE AUTHENTICATION
// =========================================================

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })

    // -----------------------------------------------------
    // CUSTOMER JWT AUTHENTICATION
    // -----------------------------------------------------

    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer =
                    builder.Configuration["Jwt:Issuer"],

                ValidAudience =
                    builder.Configuration["Jwt:Audience"],

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey)),

                ClockSkew = TimeSpan.Zero
            };

        // TOKEN VERSION VALIDATION

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                string? userIdValue =
                    context.Principal?
                        .FindFirstValue(
                            ClaimTypes.NameIdentifier);

                string? tokenVersionValue =
                    context.Principal?
                        .FindFirstValue("token_version");

                if (!int.TryParse(userIdValue, out int userId) ||
                    !int.TryParse(
                        tokenVersionValue,
                        out int tokenVersion))
                {
                    context.Fail(
                        "Invalid authentication token.");

                    return;
                }

                var dbContext =
                    context.HttpContext.RequestServices
                        .GetRequiredService<AppDbContext>();

                var user =
                    await dbContext.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            u => u.Id == userId && u.IsActive);

                if (user == null)
                {
                    context.Fail(
                        "Invalid authentication token.");

                    return;
                }

                if (user.TokenVersion != tokenVersion)
                {
                    context.Fail(
                        "Authentication token has been revoked.");

                    return;
                }
            }
        };
    })

    // -----------------------------------------------------
    // ADMIN COOKIE AUTHENTICATION
    // -----------------------------------------------------

    .AddCookie("AdminCookie", options =>
    {
        options.LoginPath =
            "/Admin/AdminAccount/Login";

        options.AccessDeniedPath =
            "/Admin/AdminAccount/Login";

        options.Cookie.Name =
            "SpendWise.Admin";

        options.Cookie.HttpOnly = true;

        options.Cookie.SecurePolicy =
            CookieSecurePolicy.Always;

        options.Cookie.SameSite =
            SameSiteMode.Strict;

        options.ExpireTimeSpan =
            TimeSpan.FromMinutes(30);

        options.SlidingExpiration = true;
    });


// =========================================================
// AUTHORIZATION
// =========================================================

builder.Services.AddAuthorization();


// =========================================================
// RATE LIMITING
// =========================================================

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter(
        "ForgotPasswordPolicy",
        limiterOptions =>
        {
            limiterOptions.PermitLimit = 3;

            limiterOptions.Window =
                TimeSpan.FromMinutes(10);

            limiterOptions.QueueLimit = 0;

            limiterOptions.QueueProcessingOrder =
                QueueProcessingOrder.OldestFirst;
        });
});


// =========================================================
// API CONTROLLERS AND ADMIN MVC VIEWS
// =========================================================

builder.Services.AddControllersWithViews();


// =========================================================
// SWAGGER
// =========================================================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter your JWT token."
        });

    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
});


// =========================================================
// BUILD APPLICATION
// =========================================================

var app = builder.Build();


// =========================================================
// SYSTEM OWNER ACCOUNT BOOTSTRAP
// =========================================================

if (builder.Configuration.GetValue<bool>(
        "AdminBootstrap:Enabled"))
{
    app.Logger.LogWarning(
        "ADMIN BOOTSTRAP IS RUNNING.");

    string fullName =
        builder.Configuration["AdminBootstrap:FullName"]?
            .Trim() ?? "";

    string email =
        builder.Configuration["AdminBootstrap:Email"]?
            .Trim()
            .ToLowerInvariant() ?? "";

    string password =
        builder.Configuration["AdminBootstrap:Password"] ?? "";

    if (string.IsNullOrWhiteSpace(fullName) ||
        string.IsNullOrWhiteSpace(email) ||
        string.IsNullOrWhiteSpace(password))
    {
        throw new InvalidOperationException(
            "Admin bootstrap is enabled, but FullName, Email or Password is missing.");
    }

    await using var scope =
        app.Services.CreateAsyncScope();

    var dbContext =
        scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

    var passwordHasher =
        scope.ServiceProvider
            .GetRequiredService<
                IPasswordHasher<AdminUser>>();

    var admin =
        await dbContext.AdminUsers
            .FirstOrDefaultAsync(a => a.Email == email);

    bool accountCreated = false;

    if (admin == null)
    {
        var existingAdmins =
            await dbContext.AdminUsers.ToListAsync();

        if (existingAdmins.Count == 1)
        {
            admin = existingAdmins[0];

            app.Logger.LogInformation(
                "One existing administrator found. Updating that account.");
        }
        else if (existingAdmins.Count > 1)
        {
            throw new InvalidOperationException(
                "Multiple administrator accounts exist and the configured email does not match any of them. Identify the correct account before continuing.");
        }
        else
        {
            admin = new AdminUser
            {
                CreatedAt = DateTime.UtcNow
            };

            dbContext.AdminUsers.Add(admin);

            accountCreated = true;
        }
    }

    admin.FullName = fullName;

    admin.Email = email;

    admin.IsActive = true;

    admin.PasswordHash =
        passwordHasher.HashPassword(
            admin,
            password);

    await dbContext.SaveChangesAsync();

    if (accountCreated)
    {
        app.Logger.LogInformation(
            "System Owner account created successfully.");
    }
    else
    {
        app.Logger.LogInformation(
            "Existing System Owner account updated successfully.");
    }
}


// =========================================================
// SWAGGER DEVELOPMENT CONFIGURATION
// =========================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI();
}


// =========================================================
// HTTPS REDIRECTION
// =========================================================

app.UseHttpsRedirection();


// =========================================================
// STATIC FILES
// =========================================================

app.UseStaticFiles();


// =========================================================
// RATE LIMITING
// =========================================================

app.UseRateLimiter();


// =========================================================
// AUTHENTICATION
// =========================================================

app.UseAuthentication();


// =========================================================
// AUTHORIZATION
// =========================================================

app.UseAuthorization();


// =========================================================
// ADMIN AREA ROUTING
// =========================================================

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=AdminDashboard}/{action=Index}/{id?}");


// =========================================================
// DEFAULT MVC ROUTING
// =========================================================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


// =========================================================
// API CONTROLLER ROUTING
// =========================================================

app.MapControllers();


// =========================================================
// RUN APPLICATION
// =========================================================

app.Run();
