using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using FluentValidation;
using DeliveryDost.Application.Configuration;
using DeliveryDost.Application.Services;
using DeliveryDost.Infrastructure.Data;
using DeliveryDost.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// ===========================================
// MVC + API Services
// ===========================================
builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation(); // Hot reload for views

builder.Services.AddEndpointsApiExplorer();

// ===========================================
// Database Configuration (SQL Server)
// ===========================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString)
);

// ===========================================
// JWT Configuration
// ===========================================
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
builder.Services.Configure<JwtSettings>(jwtSettings);

var secretKey = jwtSettings["SecretKey"];
if (string.IsNullOrEmpty(secretKey))
{
    throw new InvalidOperationException("JWT SecretKey is not configured");
}

// ===========================================
// Dual Authentication: Cookie (MVC) + JWT (API)
// ===========================================
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "JWT_OR_COOKIE";
    options.DefaultChallengeScheme = "JWT_OR_COOKIE";
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
    options.Cookie.Name = "DeliveryDost.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
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
})
.AddPolicyScheme("JWT_OR_COOKIE", "JWT_OR_COOKIE", options =>
{
    // Use JWT for API requests, Cookie for MVC requests
    options.ForwardDefaultSelector = context =>
    {
        string? authorization = context.Request.Headers["Authorization"];
        if (!string.IsNullOrEmpty(authorization) && authorization.StartsWith("Bearer "))
            return JwtBearerDefaults.AuthenticationScheme;

        // Check if request is for API endpoint
        if (context.Request.Path.StartsWithSegments("/api"))
            return JwtBearerDefaults.AuthenticationScheme;

        return CookieAuthenticationDefaults.AuthenticationScheme;
    };
});

builder.Services.AddAuthorization(options =>
{
    // Role-based policies
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("DPCMOnly", policy => policy.RequireRole("DPCM", "Admin"));
    options.AddPolicy("DPOnly", policy => policy.RequireRole("DP"));
    options.AddPolicy("BCOnly", policy => policy.RequireRole("BC", "DBC"));
    options.AddPolicy("ECOnly", policy => policy.RequireRole("EC"));
    options.AddPolicy("InspectorOnly", policy => policy.RequireRole("Inspector", "Admin"));
});

// ===========================================
// Application Services (IAM)
// ===========================================
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ITotpService, TotpService>();

// ===========================================
// KYC & Registration Services
// ===========================================
builder.Services.AddScoped<IDPRegistrationService, DPRegistrationService>();
builder.Services.AddScoped<IDuplicateDetectionService, DuplicateDetectionService>();
builder.Services.AddScoped<IAadhaarVerificationService, AadhaarVerificationService>();
builder.Services.AddScoped<IPANVerificationService, PANVerificationService>();
builder.Services.AddScoped<IBankVerificationService, BankVerificationService>();

// ===========================================
// Core Business Services
// ===========================================
builder.Services.AddScoped<IServiceAreaService, ServiceAreaService>();
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IDeliveryService, DeliveryService>();
builder.Services.AddScoped<IMatchingService, MatchingService>();
builder.Services.AddScoped<IDeliveryStateService, DeliveryStateService>();
builder.Services.AddScoped<IRatingService, RatingService>();
builder.Services.AddScoped<IComplaintService, ComplaintService>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IReferralService, ReferralService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// ===========================================
// Master Data & Reports Services
// ===========================================
builder.Services.AddScoped<IPincodeService, PincodeService>();
builder.Services.AddScoped<ISuperAdminReportService, SuperAdminReportService>();

// ===========================================
// DPCM Management Services
// ===========================================
builder.Services.AddScoped<IDPCMManagementService, DPCMManagementService>();

// ===========================================
// External API Clients (Mock for MVP)
// ===========================================
builder.Services.AddScoped<DeliveryDost.Infrastructure.Services.External.IDigiLockerClient,
    DeliveryDost.Infrastructure.Services.External.MockDigiLockerClient>();
builder.Services.AddScoped<DeliveryDost.Infrastructure.Services.External.INSDLPANClient,
    DeliveryDost.Infrastructure.Services.External.MockNSDLPANClient>();
builder.Services.AddScoped<DeliveryDost.Infrastructure.Services.External.IBankVerificationClient,
    DeliveryDost.Infrastructure.Services.External.MockBankVerificationClient>();

// ===========================================
// Utilities
// ===========================================
builder.Services.AddScoped<DeliveryDost.Infrastructure.Utilities.IEncryptionHelper,
    DeliveryDost.Infrastructure.Utilities.EncryptionHelper>();
builder.Services.AddScoped<DeliveryDost.Infrastructure.Utilities.INameMatchHelper,
    DeliveryDost.Infrastructure.Utilities.NameMatchHelper>();

// ===========================================
// Validation
// ===========================================
builder.Services.AddValidatorsFromAssemblyContaining<DeliveryDost.Application.Validators.LoginRequestValidator>();

// ===========================================
// CORS (for API consumers)
// ===========================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ===========================================
// Session Support (for multi-step registration wizard)
// ===========================================
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.Name = "DeliveryDost.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ===========================================
// Build Application
// ===========================================
var app = builder.Build();

// ===========================================
// HTTP Request Pipeline
// ===========================================
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseCors("AllowAll");

app.UseSession(); // Enable session for multi-step wizards

app.UseAuthentication();
app.UseAuthorization();

// ===========================================
// Route Configuration
// ===========================================

// MVC Routes (for web pages)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// API Routes (for mobile/external consumers)
app.MapControllers();

app.Run();
