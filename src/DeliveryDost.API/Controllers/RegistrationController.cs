using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DeliveryDost.Application.DTOs.Registration;
using DeliveryDost.Application.DTOs.Common;
using DeliveryDost.Infrastructure.Services;

namespace DeliveryDost.API.Controllers;

[ApiController]
[Route("api/v1/registration")]
public class RegistrationController : ControllerBase
{
    private readonly IDPRegistrationService _registrationService;
    private readonly ILogger<RegistrationController> _logger;

    public RegistrationController(
        IDPRegistrationService registrationService,
        ILogger<RegistrationController> logger)
    {
        _registrationService = registrationService;
        _logger = logger;
    }

    /// <summary>
    /// Initiate DP registration with phone number
    /// </summary>
    [HttpPost("dp/initiate")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<DPRegistrationResponse>>> InitiateRegistration(
        [FromBody] DPRegistrationRequest request,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("DP registration initiated for phone {Phone}", request.Phone);

            var result = await _registrationService.InitiateRegistrationAsync(
                request.Phone,
                request.ReferralCode,
                ct);

            if (result.Status == "ALREADY_REGISTERED")
            {
                return Conflict(new ApiResponse<DPRegistrationResponse>
                {
                    Success = false,
                    Message = result.Message,
                    Data = result
                });
            }

            return Ok(new ApiResponse<DPRegistrationResponse>
            {
                Success = true,
                Message = result.Message,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating DP registration");
            return StatusCode(500, ApiResponse<DPRegistrationResponse>.ErrorResponseObject(
                "INTERNAL_ERROR",
                "Internal server error",
                ex.Message
            ));
        }
    }

    /// <summary>
    /// Complete DP profile after phone verification
    /// </summary>
    [HttpPost("dp/profile")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<ProfileCompleteResponse>>> CompleteProfile(
        [FromBody] ProfileCompleteRequest request,
        CancellationToken ct)
    {
        try
        {
            // Get userId from JWT token
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new ApiResponse<ProfileCompleteResponse>
                {
                    Success = false,
                    Message = "Invalid user token"
                });
            }

            _logger.LogInformation("Completing profile for user {UserId}", userId);

            var result = await _registrationService.CompleteProfileAsync(userId, request, ct);

            return Ok(new ApiResponse<ProfileCompleteResponse>
            {
                Success = true,
                Message = "Profile completed successfully",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing DP profile");
            return StatusCode(500, ApiResponse<ProfileCompleteResponse>.ErrorResponseObject(
                "INTERNAL_ERROR",
                "Internal server error",
                ex.Message
            ));
        }
    }
}
