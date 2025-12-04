using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DeliverX.Application.DTOs.Registration;
using DeliverX.Application.DTOs.Common;
using DeliverX.Infrastructure.Services;

namespace DeliverX.API.Controllers;

[ApiController]
[Route("api/v1/kyc")]
[Authorize]
public class KYCController : ControllerBase
{
    private readonly IAadhaarVerificationService _aadhaarService;
    private readonly IPANVerificationService _panService;
    private readonly IBankVerificationService _bankService;
    private readonly IDPRegistrationService _registrationService;
    private readonly ILogger<KYCController> _logger;

    public KYCController(
        IAadhaarVerificationService aadhaarService,
        IPANVerificationService panService,
        IBankVerificationService bankService,
        IDPRegistrationService registrationService,
        ILogger<KYCController> logger)
    {
        _aadhaarService = aadhaarService;
        _panService = panService;
        _bankService = bankService;
        _registrationService = registrationService;
        _logger = logger;
    }

    /// <summary>
    /// Initiate Aadhaar verification via DigiLocker
    /// </summary>
    [HttpPost("aadhaar/initiate")]
    public async Task<ActionResult<ApiResponse<VerificationResult>>> InitiateAadhaarVerification(
        [FromBody] AadhaarVerificationRequest request,
        CancellationToken ct)
    {
        try
        {
            var userId = GetUserIdFromToken();
            if (userId == Guid.Empty)
                return Unauthorized();

            _logger.LogInformation("Initiating Aadhaar verification for user {UserId}", userId);

            VerificationResult result;

            if (request.Method == "DIGILOCKER")
            {
                result = await _aadhaarService.InitiateDigiLockerVerificationAsync(
                    userId,
                    request.RedirectUrl ?? "https://app.deliverx.com/kyc/callback",
                    ct);
            }
            else if (request.Method == "MANUAL_UPLOAD")
            {
                if (string.IsNullOrEmpty(request.DocumentUrl) || string.IsNullOrEmpty(request.AadhaarLast4))
                {
                    return BadRequest(new ApiResponse<VerificationResult>
                    {
                        Success = false,
                        Message = "DocumentUrl and AadhaarLast4 are required for manual upload"
                    });
                }

                result = await _aadhaarService.ManualVerificationAsync(
                    userId,
                    request.AadhaarLast4,
                    request.DocumentUrl,
                    ct);
            }
            else
            {
                return BadRequest(new ApiResponse<VerificationResult>
                {
                    Success = false,
                    Message = "Invalid verification method. Use DIGILOCKER or MANUAL_UPLOAD"
                });
            }

            return Ok(new ApiResponse<VerificationResult>
            {
                Success = result.IsSuccess,
                Message = result.Message ?? "Aadhaar verification initiated",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating Aadhaar verification");
            return StatusCode(500, ApiResponse<VerificationResult>.ErrorResponseObject(
                "INTERNAL_ERROR",
                "Internal server error",
                ex.Message
            ));
        }
    }

    /// <summary>
    /// Complete Aadhaar verification (DigiLocker callback)
    /// </summary>
    [HttpPost("aadhaar/verify")]
    public async Task<ActionResult<ApiResponse<VerificationResult>>> CompleteAadhaarVerification(
        [FromBody] AadhaarVerificationCallback request,
        CancellationToken ct)
    {
        try
        {
            var userId = GetUserIdFromToken();
            if (userId == Guid.Empty)
                return Unauthorized();

            _logger.LogInformation("Completing Aadhaar verification for user {UserId}", userId);

            // Extract code from DigiLocker callback (in mock, it's in the token)
            var code = request.DigilockerToken ?? Guid.NewGuid().ToString();

            var result = await _aadhaarService.CompleteDigiLockerVerificationAsync(userId, code, ct);

            if (!result.IsSuccess)
            {
                if (result.ErrorCode == "AADHAAR_ALREADY_USED")
                {
                    return Conflict(new ApiResponse<VerificationResult>
                    {
                        Success = false,
                        Message = result.ErrorMessage ?? "Aadhaar already used",
                        Data = result
                    });
                }

                return BadRequest(new ApiResponse<VerificationResult>
                {
                    Success = false,
                    Message = result.ErrorMessage ?? "Verification failed",
                    Data = result
                });
            }

            return Ok(new ApiResponse<VerificationResult>
            {
                Success = true,
                Message = "Aadhaar verified successfully",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing Aadhaar verification");
            return StatusCode(500, ApiResponse<VerificationResult>.ErrorResponseObject(
                "INTERNAL_ERROR",
                "Internal server error",
                ex.Message
            ));
        }
    }

    /// <summary>
    /// Verify PAN
    /// </summary>
    [HttpPost("pan/verify")]
    public async Task<ActionResult<ApiResponse<VerificationResult>>> VerifyPAN(
        [FromBody] PANVerificationRequest request,
        CancellationToken ct)
    {
        try
        {
            var userId = GetUserIdFromToken();
            if (userId == Guid.Empty)
                return Unauthorized();

            _logger.LogInformation("Verifying PAN for user {UserId}", userId);

            var result = await _panService.VerifyPANAsync(userId, request.PAN, ct);

            if (!result.IsSuccess)
            {
                return BadRequest(new ApiResponse<VerificationResult>
                {
                    Success = false,
                    Message = result.ErrorMessage ?? "PAN verification failed",
                    Data = result
                });
            }

            return Ok(new ApiResponse<VerificationResult>
            {
                Success = true,
                Message = "PAN verified successfully",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying PAN");
            return StatusCode(500, ApiResponse<VerificationResult>.ErrorResponseObject(
                "INTERNAL_ERROR",
                "Internal server error",
                ex.Message
            ));
        }
    }

    /// <summary>
    /// Verify bank account
    /// </summary>
    [HttpPost("bank/verify")]
    public async Task<ActionResult<ApiResponse<VerificationResult>>> VerifyBankAccount(
        [FromBody] BankVerificationRequest request,
        CancellationToken ct)
    {
        try
        {
            var userId = GetUserIdFromToken();
            if (userId == Guid.Empty)
                return Unauthorized();

            _logger.LogInformation("Verifying bank account for user {UserId}", userId);

            var result = await _bankService.VerifyBankAccountAsync(
                userId,
                request.AccountNumber,
                request.IFSCCode,
                request.AccountHolderName,
                request.Method,
                ct);

            if (!result.IsSuccess)
            {
                return BadRequest(new ApiResponse<VerificationResult>
                {
                    Success = false,
                    Message = result.ErrorMessage ?? "Bank verification failed",
                    Data = result
                });
            }

            return Ok(new ApiResponse<VerificationResult>
            {
                Success = true,
                Message = "Bank account verified successfully",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying bank account");
            return StatusCode(500, ApiResponse<VerificationResult>.ErrorResponseObject(
                "INTERNAL_ERROR",
                "Internal server error",
                ex.Message
            ));
        }
    }

    /// <summary>
    /// Get bank verification status
    /// </summary>
    [HttpGet("bank/{kycId}/status")]
    public async Task<ActionResult<ApiResponse<VerificationResult>>> GetBankVerificationStatus(
        Guid kycId,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Getting bank verification status for KYC {KycId}", kycId);

            var result = await _bankService.GetVerificationStatusAsync(kycId, ct);

            return Ok(new ApiResponse<VerificationResult>
            {
                Success = result.IsSuccess,
                Message = result.Message ?? "Status retrieved",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bank verification status");
            return StatusCode(500, ApiResponse<VerificationResult>.ErrorResponseObject(
                "INTERNAL_ERROR",
                "Internal server error",
                ex.Message
            ));
        }
    }

    /// <summary>
    /// Get overall KYC status for user
    /// </summary>
    [HttpGet("{userId}/status")]
    public async Task<ActionResult<ApiResponse<KYCStatusResponse>>> GetKYCStatus(
        Guid userId,
        CancellationToken ct)
    {
        try
        {
            var tokenUserId = GetUserIdFromToken();
            if (tokenUserId == Guid.Empty)
                return Unauthorized();

            // Users can only check their own status (unless admin)
            if (userId != tokenUserId && !User.IsInRole("SuperAdmin"))
            {
                return Forbid();
            }

            _logger.LogInformation("Getting KYC status for user {UserId}", userId);

            var result = await _registrationService.GetKYCStatusAsync(userId, ct);

            return Ok(new ApiResponse<KYCStatusResponse>
            {
                Success = true,
                Message = "KYC status retrieved successfully",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting KYC status");
            return StatusCode(500, ApiResponse<KYCStatusResponse>.ErrorResponseObject(
                "INTERNAL_ERROR",
                "Internal server error",
                ex.Message
            ));
        }
    }

    private Guid GetUserIdFromToken()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Guid.Empty;
        }
        return userId;
    }
}
