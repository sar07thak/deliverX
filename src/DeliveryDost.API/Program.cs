using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using FluentValidation;
using DeliveryDost.Application.Configuration;
using DeliveryDost.Application.Services;
using DeliveryDost.Infrastructure.Data;
using DeliveryDost.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Database - SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString)
);

// JWT Configuration
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
builder.Services.Configure<JwtSettings>(jwtSettings);

var secretKey = jwtSettings["SecretKey"];
if (string.IsNullOrEmpty(secretKey))
{
    throw new InvalidOperationException("JWT SecretKey is not configured");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Register application services (IAM)
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ITotpService, TotpService>();

// Register KYC & Registration services
builder.Services.AddScoped<IDPRegistrationService, DeliveryDost.Infrastructure.Services.DPRegistrationService>();
builder.Services.AddScoped<IDuplicateDetectionService, DeliveryDost.Infrastructure.Services.DuplicateDetectionService>();
builder.Services.AddScoped<IAadhaarVerificationService, DeliveryDost.Infrastructure.Services.AadhaarVerificationService>();
builder.Services.AddScoped<IPANVerificationService, DeliveryDost.Infrastructure.Services.PANVerificationService>();
builder.Services.AddScoped<IBankVerificationService, DeliveryDost.Infrastructure.Services.BankVerificationService>();

// Register Service Area & Geofencing services (F-03)
builder.Services.AddScoped<IServiceAreaService, ServiceAreaService>();

// Register Pricing & Commission services (F-04)
builder.Services.AddScoped<IPricingService, PricingService>();

// Register Delivery & Matching services (F-05)
builder.Services.AddScoped<IDeliveryService, DeliveryService>();
builder.Services.AddScoped<IMatchingService, MatchingService>();

// Register Delivery State Machine & POD services (F-06)
builder.Services.AddScoped<IDeliveryStateService, DeliveryStateService>();

// Register Ratings & Behavior Index services (F-07)
builder.Services.AddScoped<IRatingService, RatingService>();

// Register Complaints & Inspector services (F-08)
builder.Services.AddScoped<IComplaintService, ComplaintService>();

// Register Wallet, Payments & Settlements services (F-09)
builder.Services.AddScoped<IWalletService, WalletService>();

// Register Subscription & Billing services (F-10)
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();

// Register Referral & Donation services (F-11)
builder.Services.AddScoped<IReferralService, ReferralService>();

// Register Admin Dashboard services (F-12)
builder.Services.AddScoped<IDashboardService, DashboardService>();

// Register external API clients (Mock for MVP)
builder.Services.AddScoped<DeliveryDost.Infrastructure.Services.External.IDigiLockerClient, DeliveryDost.Infrastructure.Services.External.MockDigiLockerClient>();
builder.Services.AddScoped<DeliveryDost.Infrastructure.Services.External.INSDLPANClient, DeliveryDost.Infrastructure.Services.External.MockNSDLPANClient>();
builder.Services.AddScoped<DeliveryDost.Infrastructure.Services.External.IBankVerificationClient, DeliveryDost.Infrastructure.Services.External.MockBankVerificationClient>();

// Register utilities
builder.Services.AddScoped<DeliveryDost.Infrastructure.Utilities.IEncryptionHelper, DeliveryDost.Infrastructure.Utilities.EncryptionHelper>();
builder.Services.AddScoped<DeliveryDost.Infrastructure.Utilities.INameMatchHelper, DeliveryDost.Infrastructure.Utilities.NameMatchHelper>();

// Register FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<DeliveryDost.Application.Validators.LoginRequestValidator>();

// Add CORS (configure as needed)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
