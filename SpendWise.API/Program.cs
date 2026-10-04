using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SpendWise.API.Data;
using SpendWise.API.Models;
using SpendWise.API.Services;
using SpendWise.API.Services.Retailers;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);


// =========================================================
// DATABASE CONNECTION
// =========================================================

var connectionString =
    builder.Configuration.GetConnectionString(
        "DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found.");
}

builder.Services.AddDbContext<AppDbContext>(
    options =>
        options.UseMySql(
            connectionString,
            ServerVersion.AutoDetect(
                connectionString)
        ));


// =========================================================
// PASSWORD RESET SERVICE
// =========================================================

builder.Services
    .AddScoped<PasswordResetService>();


// =========================================================
// EMAIL SERVICE
// =========================================================

builder.Services
    .AddScoped<IEmailService, EmailService>();


// =========================================================
// LOYALTYHUB GROCERY PRICE API CONFIGURATION
// =========================================================
//
// Configuration is loaded from:
//
// LoyaltyHub:ApiKey
// LoyaltyHub:BaseUrl
//
// During development these values are stored in
// ASP.NET Core User Secrets.
//
// The API key must never be placed in the MAUI client.
// =========================================================

builder.Services
    .Configure<GroceryPriceApiOptions>(
        builder.Configuration.GetSection(
            GroceryPriceApiOptions.SectionName));


// =========================================================
// GROCERY PRICE API SERVICE
// =========================================================
//
// This service communicates with the external grocery-price
// API and retrieves product information for:
//
// - Pick n Pay
// - Checkers
// - Shoprite
//
// SpendWise never generates a missing retailer price.
// =========================================================

builder.Services
    .AddHttpClient<GroceryPriceApiService>(
        client =>
        {
            client.Timeout =
                TimeSpan.FromSeconds(30);

            client.DefaultRequestHeaders
                .UserAgent
                .ParseAdd(
                    "SpendWise/1.0");

            client.DefaultRequestHeaders
                .Accept
                .ParseAdd(
                    "application/json");
        });


// =========================================================
// RETAILER PRICE COMPARISON SERVICES
// =========================================================
//
// PriceComparisonService currently coordinates providers
// implementing IRetailerPriceProvider.
//
// The existing Pick n Pay provider is kept temporarily so
// the project remains compatible while the new grocery API
// integration is tested.
//
// Once the new API has been verified successfully, we will
// connect PriceComparisonService directly to the new source
// for Pick n Pay, Checkers and Shoprite.
// =========================================================

builder.Services
    .AddScoped<PriceComparisonService>();

builder.Services
    .AddHttpClient<
        IRetailerPriceProvider,
        PickNPayPriceProvider>(
        client =>
        {
            client.BaseAddress =
                new Uri(
                    "https://www.pnp.co.za/");

            client.Timeout =
                TimeSpan.FromSeconds(20);

            client.DefaultRequestHeaders
                .UserAgent
                .ParseAdd(
                    "SpendWise/1.0");

            client.DefaultRequestHeaders
                .Accept
                .ParseAdd(
                    "text/html,application/xhtml+xml");
        });


// =========================================================
// JWT AUTHENTICATION
// =========================================================

var jwtKey =
    builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "JWT signing key was not found.");
}

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
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
                    builder.Configuration[
                        "Jwt:Issuer"],

                ValidAudience =
                    builder.Configuration[
                        "Jwt:Audience"],

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            jwtKey)
                    ),

                ClockSkew =
                    TimeSpan.Zero
            };


        // =================================================
        // TOKEN VERSION VALIDATION
        // =================================================

        options.Events =
            new JwtBearerEvents
            {
                OnTokenValidated =
                    async context =>
                    {
                        string? userIdValue =
                            context.Principal?
                                .FindFirstValue(
                                    ClaimTypes
                                        .NameIdentifier);

                        string? tokenVersionValue =
                            context.Principal?
                                .FindFirstValue(
                                    "token_version");


                        // Token must contain both values.

                        if (!int.TryParse(
                                userIdValue,
                                out int userId) ||
                            !int.TryParse(
                                tokenVersionValue,
                                out int tokenVersion))
                        {
                            context.Fail(
                                "Invalid authentication token.");

                            return;
                        }


                        // Get database context.

                        var dbContext =
                            context.HttpContext
                                .RequestServices
                                .GetRequiredService<
                                    AppDbContext>();


                        // Get current user information.

                        var user =
                            await dbContext.Users
                                .AsNoTracking()
                                .FirstOrDefaultAsync(
                                    u =>
                                        u.Id ==
                                        userId &&
                                        u.IsActive);


                        if (user == null)
                        {
                            context.Fail(
                                "Invalid authentication token.");

                            return;
                        }


                        // Compare the JWT version with
                        // the current database version.

                        if (user.TokenVersion !=
                            tokenVersion)
                        {
                            context.Fail(
                                "Authentication token has been revoked.");

                            return;
                        }
                    }
            };
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


    // Maximum 3 forgot-password requests
    // within 10 minutes.

    options.AddFixedWindowLimiter(
        "ForgotPasswordPolicy",
        limiterOptions =>
        {
            limiterOptions.PermitLimit =
                3;

            limiterOptions.Window =
                TimeSpan.FromMinutes(10);

            limiterOptions.QueueLimit =
                0;

            limiterOptions.QueueProcessingOrder =
                QueueProcessingOrder
                    .OldestFirst;
        });
});


// =========================================================
// CONTROLLERS
// =========================================================

builder.Services.AddControllers();


// =========================================================
// SWAGGER
// =========================================================

builder.Services
    .AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(
    options =>
    {
        options.AddSecurityDefinition(
            "Bearer",
            new OpenApiSecurityScheme
            {
                Name =
                    "Authorization",

                Type =
                    SecuritySchemeType.Http,

                Scheme =
                    "bearer",

                BearerFormat =
                    "JWT",

                In =
                    ParameterLocation.Header,

                Description =
                    "Enter your JWT token."
            });


        options.AddSecurityRequirement(
            new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference =
                            new OpenApiReference
                            {
                                Type =
                                    ReferenceType
                                        .SecurityScheme,

                                Id =
                                    "Bearer"
                            }
                    },

                    Array.Empty<string>()
                }
            });
    });


var app = builder.Build();


// =========================================================
// VALIDATE LOYALTYHUB DEVELOPMENT CONFIGURATION
// =========================================================
//
// Do not print the API key.
// We only check whether configuration exists.
// =========================================================

if (app.Environment.IsDevelopment())
{
    string? loyaltyHubApiKey =
        builder.Configuration[
            "LoyaltyHub:ApiKey"];

    if (string.IsNullOrWhiteSpace(
            loyaltyHubApiKey))
    {
        app.Logger.LogWarning(
            "LoyaltyHub API key is not configured. " +
            "Add LoyaltyHub:ApiKey using ASP.NET Core User Secrets.");
    }
    else
    {
        app.Logger.LogInformation(
            "LoyaltyHub grocery-price API configuration loaded successfully.");
    }
}


// =========================================================
// SEED RETAILERS
// =========================================================

using (var scope =
    app.Services.CreateScope())
{
    var dbContext =
        scope.ServiceProvider
            .GetRequiredService<AppDbContext>();


    string[] retailerNames =
    {
        "Pick n Pay",
        "Shoprite",
        "Checkers",
        "SPAR",
        "Woolworths Food"
    };


    foreach (string retailerName in retailerNames)
    {
        bool retailerExists =
            await dbContext
                .Retailers
                .AnyAsync(
                    r =>
                        r.Name ==
                        retailerName);


        if (!retailerExists)
        {
            dbContext
                .Retailers
                .Add(
                    new Retailer
                    {
                        Name =
                            retailerName,

                        IsActive =
                            true,

                        CreatedAt =
                            DateTime.UtcNow
                    });
        }
    }


    await dbContext
        .SaveChangesAsync();
}


// =========================================================
// HTTP PIPELINE
// =========================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI();
}


app.UseHttpsRedirection();


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
// CONTROLLERS
// =========================================================

app.MapControllers();


app.Run();