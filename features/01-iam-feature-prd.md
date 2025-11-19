# Feature PRD: Identity & Access Management (IAM)

**Feature ID:** F-01
**Version:** 1.0
**Priority:** P0 (Critical - MVP Core)
**Estimated Effort:** 3-4 weeks
**Dependencies:** None (Foundation module)

---

## 1. Feature Overview

### Purpose
Provide secure, role-based authentication and authorization for all users of the DeliverX Network platform, ensuring that each user type (Super Admin, DPCM, DP, DBC, EC, Inspector) has appropriate access to features and data.

### Success Criteria
- 100% of API endpoints protected with authentication
- Zero unauthorized access incidents
- User login success rate > 99%
- Token refresh mechanism working seamlessly
- Session management with device tracking

---

## 2. User Stories

### US-IAM-001: User Authentication
**As a** Delivery Partner
**I want to** log in using my phone number and OTP
**So that** I can access the platform securely without remembering passwords

**Acceptance Criteria:**
- Phone number validation (10 digits, Indian format)
- OTP sent within 3 seconds
- OTP valid for 5 minutes
- Maximum 3 OTP attempts per 15-minute window
- Rate limiting: 5 OTP requests per hour per phone

### US-IAM-002: Multi-Method Authentication
**As a** Business Consumer
**I want to** log in using email/password with optional 2FA
**So that** I have secure access to my business account

**Acceptance Criteria:**
- Email validation and verification flow
- Password strength requirements (min 8 chars, 1 uppercase, 1 number, 1 special)
- 2FA via TOTP (Google Authenticator compatible)
- Account lockout after 5 failed attempts

### US-IAM-003: Role-Based Access Control
**As a** System Administrator
**I want to** assign granular permissions to different user roles
**So that** users only access features they are authorized for

**Acceptance Criteria:**
- Permission matrix implemented for all roles
- API endpoints check permissions before execution
- UI elements hidden/disabled based on permissions
- Audit log for permission changes

### US-IAM-004: Token Management
**As a** Mobile App User
**I want** my session to remain active securely
**So that** I don't have to re-login frequently

**Acceptance Criteria:**
- JWT access token (15-minute expiry)
- Refresh token (7-day expiry)
- Automatic token refresh before expiry
- Token rotation on refresh
- Revocation capability for compromised tokens

### US-IAM-005: Multi-Device Session Management
**As a** User
**I want to** view and manage my active sessions across devices
**So that** I can log out from lost or stolen devices

**Acceptance Criteria:**
- View all active sessions (device type, location, last active)
- Revoke individual sessions
- "Log out all devices" option
- Session limit configurable per role

---

## 3. Detailed Requirements

### 3.1 Authentication Methods

#### Phone + OTP (For DP, EC)
```
Flow:
1. User enters phone number
2. System validates format and checks rate limits
3. Generate 6-digit OTP, store hashed in Redis (5-min TTL)
4. Send OTP via SMS gateway (Twilio/MSG91)
5. User enters OTP
6. System validates OTP
7. Issue JWT access + refresh tokens
8. Return tokens to client
```

**Rate Limiting:**
- 5 OTP requests per phone per hour
- 3 verification attempts per OTP
- Exponential backoff on failures

#### Email + Password + 2FA (For DBC, DPCM, Admin, Inspector)
```
Flow:
1. User enters email + password
2. System validates credentials against hashed password
3. If 2FA enabled, prompt for TOTP code
4. Validate TOTP (allow 30-second drift)
5. Issue JWT tokens
6. Log successful login
```

**Password Policy:**
- Minimum 8 characters
- At least 1 uppercase, 1 lowercase, 1 digit, 1 special char
- Password history: cannot reuse last 5 passwords
- Mandatory reset every 90 days for admin roles

### 3.2 Role & Permission Matrix

| Role | Permissions |
|------|-------------|
| **Super Admin** | ALL |
| **DPCM** | Manage own DPs, view settlements, assign deliveries, view dashboards |
| **DP** | Accept deliveries, update status, upload POD, view earnings, manage wallet |
| **DBC** | Create deliveries, track orders, manage API keys, view invoices |
| **EC** | Create personal deliveries, track orders, rate DPs, manage saved addresses |
| **Inspector** | View assigned complaints, upload evidence, submit verdicts |

**Permission Examples:**
- `delivery.create`
- `delivery.assign`
- `delivery.accept`
- `delivery.track`
- `user.manage`
- `kyc.approve`
- `wallet.withdraw`
- `report.view`

### 3.3 JWT Token Structure

**Access Token Payload:**
```json
{
  "sub": "user-uuid",
  "role": "DP",
  "permissions": ["delivery.accept", "delivery.update", "wallet.view"],
  "dpcmId": "dpcm-uuid-if-applicable",
  "deviceId": "device-fingerprint",
  "iat": 1699012345,
  "exp": 1699013245
}
```

**Refresh Token:**
- Opaque token (UUID) stored in DB with user association
- 7-day expiry
- One-time use (rotate on refresh)
- Store device info and IP for security

### 3.4 Session Management

**Session Data:**
```csharp
public class UserSession
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string RefreshToken { get; set; } // Hashed
    public string DeviceType { get; set; } // iOS, Android, Web
    public string DeviceId { get; set; }
    public string IpAddress { get; set; }
    public string UserAgent { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastActiveAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
}
```

**Features:**
- Track last 10 active sessions per user
- Auto-revoke oldest session if limit exceeded
- Admin can force-revoke sessions
- Suspicious activity detection (IP change, location anomaly)

---

## 4. API Specifications

### 4.1 Send OTP
```http
POST /api/v1/auth/otp/send
Content-Type: application/json

Request:
{
  "phone": "9876543210",
  "countryCode": "+91"
}

Response (200):
{
  "status": "OTP_SENT",
  "expiresIn": 300,
  "message": "OTP sent to +91-9876543210"
}

Response (429 - Rate Limited):
{
  "code": "RATE_LIMIT_EXCEEDED",
  "message": "Too many OTP requests. Try again in 45 minutes.",
  "retryAfter": 2700
}
```

### 4.2 Verify OTP
```http
POST /api/v1/auth/otp/verify
Content-Type: application/json

Request:
{
  "phone": "9876543210",
  "otp": "123456",
  "deviceId": "device-fingerprint-hash"
}

Response (200):
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "550e8400-e29b-41d4-a716-446655440000",
  "expiresIn": 900,
  "user": {
    "id": "user-uuid",
    "role": "DP",
    "phone": "+91-9876543210",
    "name": "Ravi Kumar",
    "profileComplete": true
  }
}

Response (401 - Invalid OTP):
{
  "code": "INVALID_OTP",
  "message": "Invalid OTP. 2 attempts remaining.",
  "attemptsRemaining": 2
}
```

### 4.3 Email/Password Login
```http
POST /api/v1/auth/login
Content-Type: application/json

Request:
{
  "email": "business@example.com",
  "password": "SecurePass123!",
  "totpCode": "123456" // Optional, required if 2FA enabled
}

Response (200):
{
  "accessToken": "...",
  "refreshToken": "...",
  "expiresIn": 900,
  "requires2FA": false,
  "user": {
    "id": "user-uuid",
    "role": "DBC",
    "email": "business@example.com",
    "name": "ABC Store"
  }
}
```

### 4.4 Refresh Token
```http
POST /api/v1/auth/refresh
Content-Type: application/json
Authorization: Bearer <old-access-token>

Request:
{
  "refreshToken": "550e8400-e29b-41d4-a716-446655440000"
}

Response (200):
{
  "accessToken": "new-access-token",
  "refreshToken": "new-refresh-token",
  "expiresIn": 900
}

Response (401 - Invalid/Expired):
{
  "code": "INVALID_REFRESH_TOKEN",
  "message": "Refresh token invalid or expired. Please login again."
}
```

### 4.5 Logout
```http
POST /api/v1/auth/logout
Authorization: Bearer <access-token>

Request:
{
  "refreshToken": "550e8400-e29b-41d4-a716-446655440000",
  "logoutAll": false
}

Response (200):
{
  "message": "Logged out successfully"
}
```

### 4.6 Get Active Sessions
```http
GET /api/v1/auth/sessions
Authorization: Bearer <access-token>

Response (200):
{
  "sessions": [
    {
      "id": "session-uuid",
      "deviceType": "Android",
      "deviceId": "hash",
      "ipAddress": "103.x.x.x",
      "location": "Jaipur, India",
      "lastActive": "2025-11-14T10:30:00Z",
      "createdAt": "2025-11-10T08:00:00Z",
      "isCurrent": true
    }
  ]
}
```

### 4.7 Revoke Session
```http
DELETE /api/v1/auth/sessions/{sessionId}
Authorization: Bearer <access-token>

Response (200):
{
  "message": "Session revoked successfully"
}
```

---

## 5. Database Schema

```sql
-- Users table (base for all user types)
CREATE TABLE Users (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Phone NVARCHAR(15) UNIQUE NULL,
    Email NVARCHAR(255) UNIQUE NULL,
    PasswordHash NVARCHAR(255) NULL,
    Role NVARCHAR(50) NOT NULL, -- DP, DPCM, DBC, EC, Inspector, SuperAdmin
    Is2FAEnabled BIT DEFAULT 0,
    TotpSecret NVARCHAR(255) NULL, -- Encrypted
    IsActive BIT DEFAULT 1,
    IsEmailVerified BIT DEFAULT 0,
    IsPhoneVerified BIT DEFAULT 0,
    LastLoginAt DATETIME2 NULL,
    PasswordChangedAt DATETIME2 NULL,
    FailedLoginAttempts INT DEFAULT 0,
    LockedUntil DATETIME2 NULL,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),

    CONSTRAINT CK_Users_ContactMethod CHECK (Phone IS NOT NULL OR Email IS NOT NULL),
    INDEX IX_Users_Phone (Phone),
    INDEX IX_Users_Email (Email),
    INDEX IX_Users_Role (Role)
);

-- User Sessions
CREATE TABLE UserSessions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES Users(Id) ON DELETE CASCADE,
    RefreshTokenHash NVARCHAR(255) NOT NULL,
    DeviceType NVARCHAR(50), -- iOS, Android, Web
    DeviceId NVARCHAR(255),
    IpAddress NVARCHAR(45),
    UserAgent NVARCHAR(500),
    Location NVARCHAR(255),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    LastActiveAt DATETIME2 DEFAULT GETUTCDATE(),
    ExpiresAt DATETIME2 NOT NULL,
    IsRevoked BIT DEFAULT 0,
    RevokedAt DATETIME2 NULL,

    INDEX IX_UserSessions_UserId (UserId),
    INDEX IX_UserSessions_RefreshToken (RefreshTokenHash),
    INDEX IX_UserSessions_ExpiresAt (ExpiresAt)
);

-- Permissions (predefined)
CREATE TABLE Permissions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code NVARCHAR(100) NOT NULL UNIQUE, -- delivery.create, user.manage, etc.
    Name NVARCHAR(255) NOT NULL,
    Description NVARCHAR(500),
    Category NVARCHAR(50), -- Delivery, User, Wallet, etc.
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- Role-Permission Mapping
CREATE TABLE RolePermissions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Role NVARCHAR(50) NOT NULL,
    PermissionId UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES Permissions(Id),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),

    UNIQUE(Role, PermissionId),
    INDEX IX_RolePermissions_Role (Role)
);

-- OTP Storage (Redis alternative or temporary SQL table)
CREATE TABLE OTPVerifications (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Phone NVARCHAR(15) NOT NULL,
    OTPHash NVARCHAR(255) NOT NULL,
    Attempts INT DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    ExpiresAt DATETIME2 NOT NULL,
    IsVerified BIT DEFAULT 0,

    INDEX IX_OTP_Phone_Expires (Phone, ExpiresAt)
);

-- Audit Logs for IAM events
CREATE TABLE AuthAuditLogs (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NULL FOREIGN KEY REFERENCES Users(Id),
    EventType NVARCHAR(50) NOT NULL, -- LOGIN_SUCCESS, LOGIN_FAILED, OTP_SENT, SESSION_REVOKED, etc.
    Phone NVARCHAR(15) NULL,
    Email NVARCHAR(255) NULL,
    IpAddress NVARCHAR(45),
    UserAgent NVARCHAR(500),
    Details NVARCHAR(MAX), -- JSON for additional info
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),

    INDEX IX_AuthAuditLogs_UserId (UserId),
    INDEX IX_AuthAuditLogs_EventType (EventType),
    INDEX IX_AuthAuditLogs_CreatedAt (CreatedAt)
);
```

---

## 6. Development Implementation Guide

### 6.1 Technology Stack
- **Framework:** ASP.NET Core 8.0 Web API
- **Authentication:** ASP.NET Core Identity (customized) + JWT Bearer
- **OTP Service:** Twilio / MSG91
- **2FA:** OtpNet library (TOTP)
- **Password Hashing:** BCrypt.NET or PBKDF2
- **Token Storage:** Redis (for OTP) + SQL Server (for refresh tokens)
- **Rate Limiting:** AspNetCoreRateLimit middleware

### 6.2 Project Structure
```
DeliverX.IAM/
├── Controllers/
│   └── AuthController.cs
├── Services/
│   ├── IAuthService.cs
│   ├── AuthService.cs
│   ├── IOtpService.cs
│   ├── OtpService.cs
│   ├── ITokenService.cs
│   ├── TokenService.cs
│   └── ISessionService.cs
├── Models/
│   ├── DTOs/
│   │   ├── OtpSendRequest.cs
│   │   ├── OtpVerifyRequest.cs
│   │   ├── LoginRequest.cs
│   │   └── TokenResponse.cs
│   └── Entities/
│       ├── User.cs
│       ├── UserSession.cs
│       └── Permission.cs
├── Middleware/
│   ├── JwtMiddleware.cs
│   └── PermissionMiddleware.cs
├── Validators/
│   └── AuthRequestValidators.cs
└── Configuration/
    └── JwtSettings.cs
```

### 6.3 Core Service Implementation

#### AuthService.cs (Sample)
```csharp
public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IOtpService _otpService;
    private readonly ISessionService _sessionService;
    private readonly ILogger<AuthService> _logger;

    public async Task<Result<string>> SendOtpAsync(string phone, CancellationToken ct)
    {
        // 1. Validate phone format
        if (!PhoneValidator.IsValid(phone))
            return Result<string>.Failure("Invalid phone number format");

        // 2. Check rate limit (5 per hour)
        var rateLimitKey = $"otp:ratelimit:{phone}";
        var requestCount = await _redis.GetAsync<int>(rateLimitKey);
        if (requestCount >= 5)
            return Result<string>.Failure("Rate limit exceeded. Try again later.");

        // 3. Generate 6-digit OTP
        var otp = GenerateOtp();
        var otpHash = HashOtp(otp);

        // 4. Store in Redis (5 min TTL)
        var otpKey = $"otp:{phone}";
        await _redis.SetAsync(otpKey, new OtpData
        {
            Hash = otpHash,
            Attempts = 0
        }, TimeSpan.FromMinutes(5));

        // 5. Increment rate limit counter
        await _redis.IncrementAsync(rateLimitKey);
        await _redis.ExpireAsync(rateLimitKey, TimeSpan.FromHours(1));

        // 6. Send OTP via SMS gateway
        await _smsService.SendAsync(phone, $"Your DeliverX OTP is: {otp}. Valid for 5 minutes.");

        // 7. Log event
        _logger.LogInformation("OTP sent to {Phone}", phone);
        await _auditService.LogAsync("OTP_SENT", phone: phone);

        return Result<string>.Success("OTP sent successfully");
    }

    public async Task<Result<TokenResponse>> VerifyOtpAsync(string phone, string otp, string deviceId, CancellationToken ct)
    {
        // 1. Fetch OTP from Redis
        var otpKey = $"otp:{phone}";
        var otpData = await _redis.GetAsync<OtpData>(otpKey);

        if (otpData == null)
            return Result<TokenResponse>.Failure("OTP expired or invalid");

        // 2. Check attempts
        if (otpData.Attempts >= 3)
        {
            await _redis.DeleteAsync(otpKey);
            return Result<TokenResponse>.Failure("Maximum attempts exceeded");
        }

        // 3. Verify OTP
        if (!VerifyOtpHash(otp, otpData.Hash))
        {
            otpData.Attempts++;
            await _redis.SetAsync(otpKey, otpData, TimeSpan.FromMinutes(5));
            return Result<TokenResponse>.Failure($"Invalid OTP. {3 - otpData.Attempts} attempts remaining");
        }

        // 4. Delete OTP from Redis
        await _redis.DeleteAsync(otpKey);

        // 5. Get or create user
        var user = await _userManager.FindByPhoneAsync(phone);
        if (user == null)
        {
            user = new User { Phone = phone, IsPhoneVerified = true };
            await _userManager.CreateAsync(user);
        }

        // 6. Generate tokens
        var tokens = await _tokenService.GenerateTokensAsync(user, deviceId);

        // 7. Create session
        await _sessionService.CreateSessionAsync(user.Id, tokens.RefreshToken, deviceId, /* IP, UserAgent */);

        // 8. Log success
        await _auditService.LogAsync("LOGIN_SUCCESS", userId: user.Id, phone: phone);

        return Result<TokenResponse>.Success(tokens);
    }

    // Additional methods...
}
```

#### TokenService.cs (Sample)
```csharp
public class TokenService : ITokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly UserManager<User> _userManager;

    public async Task<TokenResponse> GenerateTokensAsync(User user, string deviceId)
    {
        // 1. Get user permissions
        var permissions = await GetUserPermissionsAsync(user.Role);

        // 2. Create claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("deviceId", deviceId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Add permissions as claims
        foreach (var permission in permissions)
        {
            claims.Add(new Claim("permission", permission));
        }

        // 3. Generate JWT
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(15);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        // 4. Generate refresh token
        var refreshToken = Guid.NewGuid().ToString();

        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = 900,
            User = MapToUserDto(user)
        };
    }
}
```

### 6.4 Middleware for Permission Checking

```csharp
public class PermissionMiddleware
{
    private readonly RequestDelegate _next;

    public async Task InvokeAsync(HttpContext context, IPermissionService permissionService)
    {
        var endpoint = context.GetEndpoint();
        var requiredPermission = endpoint?.Metadata.GetMetadata<RequirePermissionAttribute>();

        if (requiredPermission != null)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var hasPermission = context.User.HasClaim("permission", requiredPermission.Permission);

            if (!hasPermission)
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new
                {
                    code = "FORBIDDEN",
                    message = "You don't have permission to access this resource"
                });
                return;
            }
        }

        await _next(context);
    }
}

// Usage in controller:
[HttpPost("deliveries")]
[RequirePermission("delivery.create")]
public async Task<IActionResult> CreateDelivery([FromBody] CreateDeliveryRequest request)
{
    // Implementation
}
```

### 6.5 Configuration (appsettings.json)
```json
{
  "JwtSettings": {
    "SecretKey": "your-256-bit-secret-key-here",
    "Issuer": "DeliverXNetwork",
    "Audience": "DeliverXClients",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  },
  "OtpSettings": {
    "Length": 6,
    "ExpirationMinutes": 5,
    "MaxAttempts": 3,
    "RateLimitPerHour": 5
  },
  "SmsGateway": {
    "Provider": "Twilio",
    "AccountSid": "...",
    "AuthToken": "...",
    "FromNumber": "+1234567890"
  },
  "RateLimiting": {
    "EnableRateLimiting": true,
    "GeneralRules": [
      {
        "Endpoint": "/api/v1/auth/otp/send",
        "Period": "1h",
        "Limit": 5
      }
    ]
  }
}
```

---

## 7. Security Considerations

### 7.1 Token Security
- Store JWT secret in Azure KeyVault or AWS Secrets Manager
- Use RS256 (asymmetric) for production instead of HS256
- Implement token blacklisting for critical operations
- Short-lived access tokens (15 min) to minimize exposure

### 7.2 Password Security
- Minimum password strength enforcement
- Hash with BCrypt (cost factor 12) or Argon2
- Prevent password reuse (last 5 passwords)
- Force password reset on suspicious activity

### 7.3 Rate Limiting
- Implement at multiple layers: API Gateway + Application
- Different limits for different endpoints
- IP-based + user-based limits
- Exponential backoff for repeated failures

### 7.4 OTP Security
- Use cryptographically secure random number generator
- Hash OTP before storage
- Limited attempts (3)
- Short validity (5 minutes)
- Anti-enumeration (don't reveal if phone exists)

---

## 8. Testing Strategy

### 8.1 Unit Tests
```csharp
[Fact]
public async Task SendOtp_ValidPhone_ReturnsSuccess()
{
    // Arrange
    var phone = "9876543210";
    var authService = CreateAuthService();

    // Act
    var result = await authService.SendOtpAsync(phone, CancellationToken.None);

    // Assert
    Assert.True(result.IsSuccess);
    _mockSmsService.Verify(x => x.SendAsync(phone, It.IsAny<string>()), Times.Once);
}

[Fact]
public async Task VerifyOtp_InvalidOtp_ReturnsFailure()
{
    // Test implementation
}

[Fact]
public async Task Login_LockedAccount_ReturnsForbidden()
{
    // Test implementation
}
```

### 8.2 Integration Tests
- Test full OTP flow end-to-end
- Test token refresh mechanism
- Test session management and revocation
- Test permission enforcement

### 8.3 Security Tests
- Test JWT token tampering detection
- Test rate limiting effectiveness
- Test brute force protection
- Test session hijacking prevention

### 8.4 Load Tests
- 1000 concurrent OTP requests
- 10,000 token validations per second
- Session cleanup under load

---

## 9. Monitoring & Metrics

### Key Metrics to Track:
- Login success rate (target: >99%)
- OTP delivery rate (target: >98%)
- Token refresh failures
- Failed login attempts per hour
- Average authentication latency (target: <200ms)
- Active sessions count
- Rate limit triggers

### Alerts:
- Spike in failed login attempts (potential attack)
- OTP delivery failures (SMS gateway issue)
- Abnormal session creation rate
- JWT secret compromise indicators

---

## 10. Deployment Checklist

- [ ] JWT secret stored in secure vault
- [ ] SMS gateway configured and tested
- [ ] Rate limiting rules configured
- [ ] Database migrations applied
- [ ] Redis cache configured for OTP storage
- [ ] Audit logging enabled
- [ ] Monitoring dashboards created
- [ ] Security scanning completed (OWASP ZAP)
- [ ] Load testing completed
- [ ] Documentation published (API docs, runbooks)

---

## 11. Future Enhancements (Post-MVP)

- Biometric authentication (fingerprint, FaceID)
- Magic link login (email)
- Social login (Google, Facebook) for EC
- Adaptive authentication (risk-based)
- Passwordless authentication (WebAuthn)
- Single Sign-On (SSO) for enterprise DBC
- Advanced anomaly detection (ML-based)

---

## 12. Dependencies & Integration Points

### Depends On:
- None (foundation module)

### Used By:
- All other modules (Registration, KYC, Delivery, Wallet, etc.)

### External Integrations:
- SMS Gateway (Twilio/MSG91)
- Email Service (SendGrid/AWS SES)
- Redis (OTP & rate limiting)
- Azure KeyVault (secrets management)

---

## 13. Acceptance Criteria Summary

- [ ] DP can login with phone + OTP successfully
- [ ] DBC can login with email/password + 2FA
- [ ] Token refresh works seamlessly without re-login
- [ ] User can view and manage active sessions
- [ ] Permission-based access control enforced on all endpoints
- [ ] Rate limiting prevents abuse
- [ ] Account lockout works after failed attempts
- [ ] Audit logs capture all authentication events
- [ ] API latency meets performance targets (<200ms P95)
- [ ] Security tests pass (no critical vulnerabilities)

---

**Status:** Ready for Development
**Next Steps:** Begin implementation with Sprint 1 (2 weeks) focusing on OTP flow and basic JWT authentication
