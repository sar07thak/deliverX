using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DeliveryDost.Application.DTOs.Auth;
using DeliveryDost.Application.DTOs.Common;
using DeliveryDost.Application.Services;

namespace DeliveryDost.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Send OTP to user's phone number
    /// </summary>
    [HttpPost("otp/send")]
    [ProducesResponseType(typeof(ApiResponse<OtpSendResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> SendOtp([FromBody] OtpSendRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.SendOtpAsync(request, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "RATE_LIMIT_EXCEEDED")
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, new ErrorResponse
                {
                    Code = result.ErrorCode!,
                    Message = result.ErrorMessage!
                });
            }

            return BadRequest(new ErrorResponse
            {
                Code = result.ErrorCode ?? "OTP_SEND_FAILED",
                Message = result.ErrorMessage!
            });
        }

        return Ok(ApiResponse<OtpSendResponse>.SuccessResponse(result.Data!));
    }

    /// <summary>
    /// Verify OTP and get authentication tokens
    /// </summary>
    [HttpPost("otp/verify")]
    [ProducesResponseType(typeof(ApiResponse<TokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> VerifyOtp([FromBody] OtpVerifyRequest request, CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _authService.VerifyOtpAsync(request, ipAddress, userAgent, cancellationToken);

        if (!result.IsSuccess)
        {
            return Unauthorized(new ErrorResponse
            {
                Code = result.ErrorCode ?? "OTP_VERIFICATION_FAILED",
                Message = result.ErrorMessage!
            });
        }

        return Ok(ApiResponse<TokenResponse>.SuccessResponse(result.Data!));
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<TokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _authService.LoginAsync(request, ipAddress, userAgent, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "ACCOUNT_LOCKED")
            {
                return StatusCode(StatusCodes.Status423Locked, new ErrorResponse
                {
                    Code = result.ErrorCode,
                    Message = result.ErrorMessage!
                });
            }

            if (result.ErrorCode == "2FA_REQUIRED")
            {
                return Ok(ApiResponse<TokenResponse>.ErrorResponseObject(
                    result.ErrorCode,
                    result.ErrorMessage!,
                    new { Requires2FA = true }));
            }

            return Unauthorized(new ErrorResponse
            {
                Code = result.ErrorCode ?? "LOGIN_FAILED",
                Message = result.ErrorMessage!
            });
        }

        return Ok(ApiResponse<TokenResponse>.SuccessResponse(result.Data!));
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<TokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.RefreshTokenAsync(request.RefreshToken, cancellationToken);

        if (!result.IsSuccess)
        {
            return Unauthorized(new ErrorResponse
            {
                Code = result.ErrorCode ?? "REFRESH_FAILED",
                Message = result.ErrorMessage!
            });
        }

        return Ok(ApiResponse<TokenResponse>.SuccessResponse(result.Data!));
    }

    /// <summary>
    /// Logout user (revoke session)
    /// </summary>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized(new ErrorResponse
            {
                Code = "UNAUTHORIZED",
                Message = "User not authenticated"
            });
        }

        var result = await _authService.LogoutAsync(userId.Value, request.RefreshToken, request.LogoutAll, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new ErrorResponse
            {
                Code = result.ErrorCode ?? "LOGOUT_FAILED",
                Message = result.ErrorMessage!
            });
        }

        return Ok(new { Message = "Logged out successfully" });
    }

    /// <summary>
    /// Get all active sessions for current user
    /// </summary>
    [Authorize]
    [HttpGet("sessions")]
    [ProducesResponseType(typeof(ApiResponse<SessionListResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSessions(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized(new ErrorResponse
            {
                Code = "UNAUTHORIZED",
                Message = "User not authenticated"
            });
        }

        var deviceId = User.FindFirstValue("deviceId") ?? string.Empty;
        var result = await _authService.GetSessionsAsync(userId.Value, deviceId, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new ErrorResponse
            {
                Code = result.ErrorCode ?? "GET_SESSIONS_FAILED",
                Message = result.ErrorMessage!
            });
        }

        return Ok(ApiResponse<SessionListResponse>.SuccessResponse(result.Data!));
    }

    /// <summary>
    /// Revoke a specific session
    /// </summary>
    [Authorize]
    [HttpDelete("sessions/{sessionId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeSession([FromRoute] Guid sessionId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized(new ErrorResponse
            {
                Code = "UNAUTHORIZED",
                Message = "User not authenticated"
            });
        }

        var result = await _authService.RevokeSessionAsync(sessionId, userId.Value, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "SESSION_NOT_FOUND")
            {
                return NotFound(new ErrorResponse
                {
                    Code = result.ErrorCode,
                    Message = result.ErrorMessage!
                });
            }

            return BadRequest(new ErrorResponse
            {
                Code = result.ErrorCode ?? "REVOKE_SESSION_FAILED",
                Message = result.ErrorMessage!
            });
        }

        return Ok(new { Message = "Session revoked successfully" });
    }

    private Guid? GetUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }
}
