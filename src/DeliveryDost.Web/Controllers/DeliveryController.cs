using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DeliveryDost.Application.DTOs.Delivery;
using DeliveryDost.Application.Services;
using DeliveryDost.Web.ViewModels.Delivery;
using DeliveryDost.Web.ViewModels.Business;

namespace DeliveryDost.Web.Controllers;

/// <summary>
/// MVC Controller for delivery management
/// </summary>
[Authorize]
public class DeliveryController : Controller
{
    private readonly IDeliveryService _deliveryService;
    private readonly IMatchingService _matchingService;
    private readonly IServiceAreaService _serviceAreaService;
    private readonly ILogger<DeliveryController> _logger;

    public DeliveryController(
        IDeliveryService deliveryService,
        IMatchingService matchingService,
        IServiceAreaService serviceAreaService,
        ILogger<DeliveryController> logger)
    {
        _deliveryService = deliveryService;
        _matchingService = matchingService;
        _serviceAreaService = serviceAreaService;
        _logger = logger;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
    private string GetUserRole() => User.FindFirst(ClaimTypes.Role)?.Value ?? "";

    #region Requester Views (EC/BC/DBC)

    /// <summary>
    /// List deliveries for the requester
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "EC,BC,DBC,Admin")]
    public async Task<IActionResult> Index(string? status, DateTime? fromDate, DateTime? toDate, int page = 1)
    {
        var userId = GetUserId();

        try
        {
            var request = new DeliveryListRequest
            {
                Status = status,
                FromDate = fromDate,
                ToDate = toDate,
                Page = page,
                PageSize = 20
            };

            var response = await _deliveryService.GetDeliveriesAsync(userId, null, request);

            var model = new DeliveryListViewModel
            {
                Deliveries = response.Deliveries.Select(d => new DeliveryListItemViewModel
                {
                    Id = d.Id,
                    Status = d.Status,
                    PickupAddress = d.PickupAddress,
                    DropAddress = d.DropAddress,
                    EstimatedPrice = d.EstimatedPrice,
                    DistanceKm = d.DistanceKm,
                    AssignedDPName = d.AssignedDPName,
                    CreatedAt = d.CreatedAt,
                    Priority = d.Priority
                }).ToList(),
                TotalCount = response.TotalCount,
                Page = response.Page,
                PageSize = response.PageSize,
                StatusFilter = status,
                FromDate = fromDate,
                ToDate = toDate,
                PageTitle = "My Deliveries",
                ViewMode = "requester"
            };

            ViewData["Title"] = "My Deliveries";
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading deliveries for user {UserId}", userId);
            TempData["Error"] = "Failed to load deliveries";
            return RedirectToAction("Index", "Dashboard");
        }
    }

    /// <summary>
    /// Create new delivery form
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "EC,BC,DBC")]
    public IActionResult Create()
    {
        var model = new CreateDeliveryViewModel
        {
            // Default to Jaipur
            PickupLat = 26.9124m,
            PickupLng = 75.7873m,
            DropLat = 26.9024m,
            DropLng = 75.8073m
        };

        ViewData["Title"] = "Create Delivery";
        return View(model);
    }

    /// <summary>
    /// Create new delivery
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "EC,BC,DBC")]
    public async Task<IActionResult> Create(CreateDeliveryViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Create Delivery";
            return View(model);
        }

        var userId = GetUserId();
        var userRole = GetUserRole();

        try
        {
            var request = new CreateDeliveryRequest
            {
                RequesterId = userId,
                RequesterType = userRole,
                Pickup = new LocationInfo
                {
                    Lat = model.PickupLat,
                    Lng = model.PickupLng,
                    Address = model.PickupAddress,
                    ContactName = model.PickupContactName,
                    ContactPhone = model.PickupContactPhone,
                    Instructions = model.PickupInstructions
                },
                Drop = new LocationInfo
                {
                    Lat = model.DropLat,
                    Lng = model.DropLng,
                    Address = model.DropAddress,
                    ContactName = model.DropContactName,
                    ContactPhone = model.DropContactPhone,
                    Instructions = model.DropInstructions
                },
                Package = new PackageInfo
                {
                    WeightKg = model.WeightKg,
                    Type = model.PackageType,
                    Value = model.PackageValue,
                    Description = model.PackageDescription
                },
                Priority = model.Priority,
                ScheduledAt = model.ScheduledAt,
                SpecialInstructions = model.SpecialInstructions,
                PreferredDPId = model.PreferredDPId
            };

            var response = await _deliveryService.CreateDeliveryAsync(request, userId);

            _logger.LogInformation("Delivery {DeliveryId} created by user {UserId}", response.DeliveryId, userId);

            TempData["Success"] = "Delivery created successfully! Finding delivery partners...";
            return RedirectToAction("Details", new { id = response.DeliveryId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating delivery for user {UserId}", userId);
            ModelState.AddModelError("", "Failed to create delivery. Please try again.");
            ViewData["Title"] = "Create Delivery";
            return View(model);
        }
    }

    /// <summary>
    /// View delivery details
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        try
        {
            var delivery = await _deliveryService.GetDeliveryAsync(id);

            if (delivery == null)
            {
                TempData["Error"] = "Delivery not found";
                return RedirectToAction("Index");
            }

            var model = new DeliveryDetailsViewModel
            {
                Id = delivery.Id,
                Status = delivery.Status,
                CreatedAt = delivery.CreatedAt,
                Pickup = new LocationViewModel
                {
                    Lat = delivery.Pickup.Lat,
                    Lng = delivery.Pickup.Lng,
                    Address = delivery.Pickup.Address,
                    ContactName = delivery.Pickup.ContactName,
                    ContactPhone = delivery.Pickup.ContactPhone,
                    Instructions = delivery.Pickup.Instructions
                },
                Drop = new LocationViewModel
                {
                    Lat = delivery.Drop.Lat,
                    Lng = delivery.Drop.Lng,
                    Address = delivery.Drop.Address,
                    ContactName = delivery.Drop.ContactName,
                    ContactPhone = delivery.Drop.ContactPhone,
                    Instructions = delivery.Drop.Instructions
                },
                Package = new PackageViewModel
                {
                    WeightKg = delivery.Package.WeightKg,
                    Type = delivery.Package.Type,
                    Value = delivery.Package.Value,
                    Description = delivery.Package.Description
                },
                EstimatedPrice = delivery.Pricing.EstimatedPrice,
                FinalPrice = delivery.Pricing.FinalPrice,
                Timeline = delivery.Timeline.Select(t => new TimelineItemViewModel
                {
                    Status = t.Status,
                    Timestamp = t.Timestamp,
                    Description = t.Description
                }).ToList()
            };

            if (delivery.AssignedDP != null)
            {
                model.AssignedDP = new DPInfoViewModel
                {
                    DPId = delivery.AssignedDP.DPId,
                    Name = delivery.AssignedDP.DPName,
                    Phone = delivery.AssignedDP.DPPhone,
                    PhotoUrl = delivery.AssignedDP.DPPhoto,
                    Rating = delivery.AssignedDP.Rating
                };
            }

            // Mark timeline items as completed/current
            var statusOrder = new[] { "CREATED", "MATCHING", "ASSIGNED", "ACCEPTED", "PICKED_UP", "IN_TRANSIT", "DELIVERED" };
            var currentIndex = Array.IndexOf(statusOrder, delivery.Status);
            foreach (var item in model.Timeline)
            {
                var itemIndex = Array.IndexOf(statusOrder, item.Status);
                item.IsCompleted = itemIndex < currentIndex;
                item.IsCurrent = item.Status == delivery.Status;
            }

            ViewData["Title"] = $"Delivery #{id.ToString()[..8]}";
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading delivery {DeliveryId}", id);
            TempData["Error"] = "Failed to load delivery details";
            return RedirectToAction("Index");
        }
    }

    /// <summary>
    /// Cancel a delivery
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id, string reason)
    {
        var userId = GetUserId();

        try
        {
            var success = await _deliveryService.CancelDeliveryAsync(id, userId, reason);

            if (success)
            {
                TempData["Success"] = "Delivery cancelled successfully";
            }
            else
            {
                TempData["Error"] = "Failed to cancel delivery";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling delivery {DeliveryId}", id);
            TempData["Error"] = "Failed to cancel delivery";
        }

        return RedirectToAction("Details", new { id });
    }

    #endregion

    #region Bulk Delivery (BC/DBC)

    /// <summary>
    /// Bulk delivery upload page
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "BC,DBC")]
    public IActionResult BulkCreate()
    {
        var model = new BulkUploadViewModel();
        ViewData["Title"] = "Bulk Upload Deliveries";
        return View(model);
    }

    /// <summary>
    /// Process bulk upload CSV
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "BC,DBC")]
    public async Task<IActionResult> BulkCreate(BulkUploadViewModel model)
    {
        if (model.CsvFile == null || model.CsvFile.Length == 0)
        {
            ModelState.AddModelError("CsvFile", "Please select a CSV file to upload");
            ViewData["Title"] = "Bulk Upload Deliveries";
            return View(model);
        }

        if (!model.CsvFile.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("CsvFile", "Please upload a valid CSV file");
            ViewData["Title"] = "Bulk Upload Deliveries";
            return View(model);
        }

        var userId = GetUserId();
        var userRole = GetUserRole();

        try
        {
            var deliveries = new List<BulkDeliveryItemViewModel>();
            var errors = new List<BulkDeliveryError>();
            var rowNumber = 0;

            using (var reader = new StreamReader(model.CsvFile.OpenReadStream()))
            {
                // Skip header
                var header = await reader.ReadLineAsync();
                rowNumber++;

                while (!reader.EndOfStream)
                {
                    rowNumber++;
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var values = ParseCsvLine(line);
                    if (values.Length < 8)
                    {
                        errors.Add(new BulkDeliveryError
                        {
                            RowNumber = rowNumber,
                            Field = "Row",
                            Message = "Insufficient columns. Expected at least 8 columns."
                        });
                        continue;
                    }

                    var delivery = new BulkDeliveryItemViewModel
                    {
                        RowNumber = rowNumber,
                        PickupAddress = values[0].Trim(),
                        PickupContactName = values[1].Trim(),
                        PickupContactPhone = values[2].Trim(),
                        DropAddress = values[3].Trim(),
                        DropContactName = values[4].Trim(),
                        DropContactPhone = values[5].Trim(),
                        WeightKg = decimal.TryParse(values[6], out var weight) ? weight : 1,
                        PackageType = values.Length > 7 ? values[7].Trim() : "PARCEL",
                        Priority = values.Length > 8 ? values[8].Trim() : "STANDARD",
                        SpecialInstructions = values.Length > 9 ? values[9].Trim() : null,
                        IsValid = true
                    };

                    // Validate
                    if (string.IsNullOrWhiteSpace(delivery.PickupAddress))
                    {
                        delivery.IsValid = false;
                        delivery.ValidationError = "Pickup address is required";
                    }
                    else if (string.IsNullOrWhiteSpace(delivery.DropAddress))
                    {
                        delivery.IsValid = false;
                        delivery.ValidationError = "Drop address is required";
                    }
                    else if (string.IsNullOrWhiteSpace(delivery.DropContactPhone))
                    {
                        delivery.IsValid = false;
                        delivery.ValidationError = "Drop contact phone is required";
                    }

                    // Estimate price (simple calculation)
                    delivery.EstimatedPrice = 50 + (delivery.WeightKg * 5);

                    deliveries.Add(delivery);
                }
            }

            var validDeliveries = deliveries.Where(d => d.IsValid).ToList();
            var invalidDeliveries = deliveries.Where(d => !d.IsValid).ToList();

            // Store in session for confirmation
            var sessionKey = Guid.NewGuid().ToString();
            HttpContext.Session.SetString($"BulkDelivery_{sessionKey}",
                System.Text.Json.JsonSerializer.Serialize(validDeliveries));

            var confirmModel = new BulkDeliveryConfirmViewModel
            {
                ValidDeliveries = validDeliveries,
                TotalCount = validDeliveries.Count,
                TotalEstimatedCost = validDeliveries.Sum(d => d.EstimatedPrice),
                WalletBalance = 10000, // TODO: Get from wallet service
                SessionKey = sessionKey
            };

            if (invalidDeliveries.Any())
            {
                TempData["Warning"] = $"{invalidDeliveries.Count} rows have validation errors and will be skipped.";
            }

            ViewData["Title"] = "Confirm Bulk Deliveries";
            return View("BulkConfirm", confirmModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing bulk upload for user {UserId}", userId);
            ModelState.AddModelError("", "Failed to process the CSV file. Please check the format and try again.");
            ViewData["Title"] = "Bulk Upload Deliveries";
            return View(model);
        }
    }

    /// <summary>
    /// Confirm and create bulk deliveries
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "BC,DBC")]
    public async Task<IActionResult> BulkConfirm(string sessionKey)
    {
        var userId = GetUserId();
        var userRole = GetUserRole();

        try
        {
            var deliveriesJson = HttpContext.Session.GetString($"BulkDelivery_{sessionKey}");
            if (string.IsNullOrEmpty(deliveriesJson))
            {
                TempData["Error"] = "Session expired. Please upload the file again.";
                return RedirectToAction("BulkCreate");
            }

            var deliveries = System.Text.Json.JsonSerializer.Deserialize<List<BulkDeliveryItemViewModel>>(deliveriesJson);
            if (deliveries == null || !deliveries.Any())
            {
                TempData["Error"] = "No valid deliveries found.";
                return RedirectToAction("BulkCreate");
            }

            var result = new BulkDeliveryResultViewModel
            {
                TotalRequested = deliveries.Count
            };

            foreach (var delivery in deliveries)
            {
                try
                {
                    var request = new CreateDeliveryRequest
                    {
                        RequesterId = userId,
                        RequesterType = userRole,
                        Pickup = new LocationInfo
                        {
                            Address = delivery.PickupAddress,
                            ContactName = delivery.PickupContactName,
                            ContactPhone = delivery.PickupContactPhone
                        },
                        Drop = new LocationInfo
                        {
                            Address = delivery.DropAddress,
                            ContactName = delivery.DropContactName,
                            ContactPhone = delivery.DropContactPhone,
                            Instructions = delivery.SpecialInstructions
                        },
                        Package = new PackageInfo
                        {
                            WeightKg = delivery.WeightKg,
                            Type = delivery.PackageType
                        },
                        Priority = delivery.Priority
                    };

                    var response = await _deliveryService.CreateDeliveryAsync(request, userId);

                    result.CreatedDeliveries.Add(new BulkDeliveryCreatedItem
                    {
                        DeliveryId = response.DeliveryId,
                        RowNumber = delivery.RowNumber,
                        DropAddress = delivery.DropAddress,
                        Status = "CREATED"
                    });
                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create delivery for row {RowNumber}", delivery.RowNumber);
                    result.Errors.Add(new BulkDeliveryError
                    {
                        RowNumber = delivery.RowNumber,
                        Field = "Creation",
                        Message = ex.Message
                    });
                    result.FailedCount++;
                }
            }

            // Clear session
            HttpContext.Session.Remove($"BulkDelivery_{sessionKey}");

            _logger.LogInformation("Bulk delivery completed for user {UserId}: {Success}/{Total} created",
                userId, result.SuccessCount, result.TotalRequested);

            ViewData["Title"] = "Bulk Upload Results";
            return View("BulkResult", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming bulk deliveries for user {UserId}", userId);
            TempData["Error"] = "Failed to create deliveries. Please try again.";
            return RedirectToAction("BulkCreate");
        }
    }

    /// <summary>
    /// Download sample CSV template
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "BC,DBC")]
    public IActionResult DownloadTemplate()
    {
        var csv = new StringBuilder();
        csv.AppendLine("PickupAddress,PickupContactName,PickupContactPhone,DropAddress,DropContactName,DropContactPhone,WeightKg,PackageType,Priority,SpecialInstructions");
        csv.AppendLine("\"123 Main St, Jaipur\",John Sender,9876543210,\"456 Oak Ave, Jaipur\",Jane Receiver,9123456789,2.5,PARCEL,STANDARD,Handle with care");
        csv.AppendLine("\"789 Market Rd, Jaipur\",Business Name,9876543211,\"321 Pine St, Jaipur\",Customer Name,9123456780,1.0,DOCUMENTS,EXPRESS,");

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", "bulk-delivery-template.csv");
    }

    /// <summary>
    /// View bulk delivery history
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "BC,DBC")]
    public async Task<IActionResult> BulkHistory(int page = 1)
    {
        var userId = GetUserId();

        // For now, return empty history - can be enhanced with batch tracking
        var model = new BulkDeliveryHistoryViewModel
        {
            Batches = new List<BulkDeliveryBatchItem>(),
            TotalBatches = 0,
            Page = page,
            PageSize = 20
        };

        ViewData["Title"] = "Bulk Upload History";
        return View(model);
    }

    private string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        foreach (var c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        result.Add(current.ToString());

        return result.ToArray();
    }

    #endregion

    #region DP Views

    /// <summary>
    /// Available deliveries for DP
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "DP")]
    public async Task<IActionResult> Available()
    {
        var dpId = GetUserId();

        try
        {
            var pendingDeliveries = await _deliveryService.GetPendingDeliveriesForDPAsync(dpId);
            var availability = await _matchingService.GetDPAvailabilityAsync(dpId);

            var model = new AvailableDeliveriesViewModel
            {
                Deliveries = pendingDeliveries.Deliveries.Select(d => new AvailableDeliveryItemViewModel
                {
                    Id = d.Id,
                    PickupAddress = d.PickupAddress,
                    DropAddress = d.DropAddress,
                    DistanceKm = d.DistanceKm ?? 0,
                    EstimatedPrice = d.EstimatedPrice ?? 0,
                    Priority = d.Priority,
                    CreatedAt = d.CreatedAt,
                    MinutesRemaining = 5 // Default timeout
                }).ToList(),
                IsOnline = availability?.Status == "ONLINE",
                Availability = new DPAvailabilityViewModel
                {
                    IsOnline = availability?.Status == "ONLINE",
                    Status = availability?.Status ?? "OFFLINE",
                    ActiveDeliveries = availability?.CurrentDeliveryId.HasValue == true ? 1 : 0
                }
            };

            ViewData["Title"] = "Available Deliveries";
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading available deliveries for DP {DPId}", dpId);
            TempData["Error"] = "Failed to load available deliveries";
            return RedirectToAction("Index", "Dashboard");
        }
    }

    /// <summary>
    /// Active deliveries for DP
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "DP")]
    public async Task<IActionResult> Active()
    {
        var dpId = GetUserId();

        try
        {
            var request = new DeliveryListRequest
            {
                Status = null, // Get active statuses
                Page = 1,
                PageSize = 50
            };

            var response = await _deliveryService.GetDeliveriesAsync(null, dpId, request);

            // Filter to active statuses only
            var activeStatuses = new[] { "ACCEPTED", "PICKED_UP", "IN_TRANSIT" };
            var activeDeliveries = response.Deliveries
                .Where(d => activeStatuses.Contains(d.Status))
                .ToList();

            var model = new DeliveryListViewModel
            {
                Deliveries = activeDeliveries.Select(d => new DeliveryListItemViewModel
                {
                    Id = d.Id,
                    Status = d.Status,
                    PickupAddress = d.PickupAddress,
                    DropAddress = d.DropAddress,
                    EstimatedPrice = d.EstimatedPrice,
                    DistanceKm = d.DistanceKm,
                    CreatedAt = d.CreatedAt,
                    Priority = d.Priority
                }).ToList(),
                TotalCount = activeDeliveries.Count,
                PageTitle = "Active Deliveries",
                ViewMode = "dp"
            };

            ViewData["Title"] = "Active Deliveries";
            return View("Index", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading active deliveries for DP {DPId}", dpId);
            TempData["Error"] = "Failed to load active deliveries";
            return RedirectToAction("Index", "Dashboard");
        }
    }

    /// <summary>
    /// Delivery history for DP
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "DP")]
    public async Task<IActionResult> History(string? status, DateTime? fromDate, DateTime? toDate, int page = 1)
    {
        var dpId = GetUserId();

        try
        {
            var request = new DeliveryListRequest
            {
                Status = status,
                FromDate = fromDate,
                ToDate = toDate,
                Page = page,
                PageSize = 20
            };

            var response = await _deliveryService.GetDeliveriesAsync(null, dpId, request);

            var model = new DeliveryListViewModel
            {
                Deliveries = response.Deliveries.Select(d => new DeliveryListItemViewModel
                {
                    Id = d.Id,
                    Status = d.Status,
                    PickupAddress = d.PickupAddress,
                    DropAddress = d.DropAddress,
                    EstimatedPrice = d.EstimatedPrice,
                    DistanceKm = d.DistanceKm,
                    CreatedAt = d.CreatedAt,
                    Priority = d.Priority
                }).ToList(),
                TotalCount = response.TotalCount,
                Page = response.Page,
                PageSize = response.PageSize,
                StatusFilter = status,
                FromDate = fromDate,
                ToDate = toDate,
                PageTitle = "Delivery History",
                ViewMode = "dp"
            };

            ViewData["Title"] = "Delivery History";
            return View("Index", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading delivery history for DP {DPId}", dpId);
            TempData["Error"] = "Failed to load delivery history";
            return RedirectToAction("Index", "Dashboard");
        }
    }

    /// <summary>
    /// DP accepts a delivery
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "DP")]
    public async Task<IActionResult> Accept(Guid id)
    {
        var dpId = GetUserId();

        try
        {
            var response = await _matchingService.AcceptDeliveryAsync(id, dpId);

            if (response.IsSuccess)
            {
                TempData["Success"] = "Delivery accepted! Navigate to pickup location.";
                return RedirectToAction("Details", new { id });
            }
            else
            {
                TempData["Error"] = !string.IsNullOrEmpty(response.Message) ? response.Message : "Failed to accept delivery";
                return RedirectToAction("Available");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting delivery {DeliveryId} by DP {DPId}", id, dpId);
            TempData["Error"] = "Failed to accept delivery";
            return RedirectToAction("Available");
        }
    }

    /// <summary>
    /// DP rejects a delivery
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "DP")]
    public async Task<IActionResult> Reject(Guid id, string reason)
    {
        var dpId = GetUserId();

        try
        {
            var request = new RejectDeliveryRequest { Reason = reason };
            var success = await _matchingService.RejectDeliveryAsync(id, dpId, request);

            if (success)
            {
                TempData["Success"] = "Delivery rejected";
            }
            else
            {
                TempData["Error"] = "Failed to reject delivery";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting delivery {DeliveryId} by DP {DPId}", id, dpId);
            TempData["Error"] = "Failed to reject delivery";
        }

        return RedirectToAction("Available");
    }

    /// <summary>
    /// Toggle DP online/offline status
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "DP")]
    public async Task<IActionResult> ToggleAvailability(bool goOnline)
    {
        var dpId = GetUserId();

        try
        {
            var request = new UpdateDPAvailabilityRequest
            {
                Status = goOnline ? "ONLINE" : "OFFLINE"
            };

            await _matchingService.UpdateDPAvailabilityAsync(dpId, request);

            TempData["Success"] = goOnline ? "You are now online" : "You are now offline";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating availability for DP {DPId}", dpId);
            TempData["Error"] = "Failed to update availability";
        }

        return RedirectToAction("Available");
    }

    #endregion

    #region Lifecycle Actions (Sprint 6)

    /// <summary>
    /// Current active delivery for DP with workflow actions
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "DP")]
    public async Task<IActionResult> CurrentDelivery(Guid id)
    {
        var dpId = GetUserId();

        try
        {
            var delivery = await _deliveryService.GetDeliveryAsync(id);

            if (delivery == null)
            {
                TempData["Error"] = "Delivery not found";
                return RedirectToAction("Active");
            }

            // Build step progress
            var steps = new List<DeliveryStepViewModel>
            {
                new() { StepNumber = 1, Title = "Accepted", Icon = "bi-check-circle", IsCompleted = true },
                new() { StepNumber = 2, Title = "Picked Up", Icon = "bi-box-arrow-up", IsCompleted = delivery.Status != "ACCEPTED" },
                new() { StepNumber = 3, Title = "In Transit", Icon = "bi-truck", IsCompleted = delivery.Status is "IN_TRANSIT" or "DELIVERED" },
                new() { StepNumber = 4, Title = "Delivered", Icon = "bi-check2-all", IsCompleted = delivery.Status == "DELIVERED" }
            };

            // Mark current step
            var currentIndex = delivery.Status switch
            {
                "ACCEPTED" => 0,
                "PICKED_UP" => 1,
                "IN_TRANSIT" => 2,
                "DELIVERED" => 3,
                _ => 0
            };
            if (currentIndex < steps.Count) steps[currentIndex].IsCurrent = true;

            var model = new ActiveDeliveryViewModel
            {
                Id = delivery.Id,
                Status = delivery.Status,
                CreatedAt = delivery.CreatedAt,
                Pickup = new LocationViewModel
                {
                    Lat = delivery.Pickup.Lat,
                    Lng = delivery.Pickup.Lng,
                    Address = delivery.Pickup.Address,
                    ContactName = delivery.Pickup.ContactName,
                    ContactPhone = delivery.Pickup.ContactPhone,
                    Instructions = delivery.Pickup.Instructions
                },
                Drop = new LocationViewModel
                {
                    Lat = delivery.Drop.Lat,
                    Lng = delivery.Drop.Lng,
                    Address = delivery.Drop.Address,
                    ContactName = delivery.Drop.ContactName,
                    ContactPhone = delivery.Drop.ContactPhone,
                    Instructions = delivery.Drop.Instructions
                },
                Package = new PackageViewModel
                {
                    WeightKg = delivery.Package.WeightKg,
                    Type = delivery.Package.Type,
                    Value = delivery.Package.Value,
                    Description = delivery.Package.Description
                },
                EstimatedEarning = delivery.Pricing.EstimatedPrice,
                Steps = steps,
                CurrentStep = delivery.Status switch
                {
                    "ACCEPTED" => "Navigate to pickup location",
                    "PICKED_UP" => "Collect package from sender",
                    "IN_TRANSIT" => "Deliver to recipient",
                    "DELIVERED" => "Delivery completed",
                    _ => ""
                },
                NextAction = delivery.Status switch
                {
                    "ACCEPTED" => "Confirm Pickup",
                    "PICKED_UP" => "Start Delivery",
                    "IN_TRANSIT" => "Complete Delivery",
                    _ => ""
                },
                NextActionButton = delivery.Status switch
                {
                    "ACCEPTED" => "I've Picked Up",
                    "PICKED_UP" => "Start Transit",
                    "IN_TRANSIT" => "Complete with POD",
                    _ => ""
                }
            };

            ViewData["Title"] = "Active Delivery";
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading current delivery {DeliveryId}", id);
            TempData["Error"] = "Failed to load delivery";
            return RedirectToAction("Active");
        }
    }

    /// <summary>
    /// Update delivery status (DP workflow transitions)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "DP")]
    public async Task<IActionResult> UpdateStatus(Guid id, string newStatus, string? notes)
    {
        var dpId = GetUserId();

        try
        {
            var success = await _deliveryService.UpdateDeliveryStatusAsync(
                id, newStatus, dpId, "DP", notes);

            if (success)
            {
                var message = newStatus switch
                {
                    "PICKED_UP" => "Package picked up! Start heading to drop location.",
                    "IN_TRANSIT" => "You're now in transit. Deliver safely!",
                    _ => "Status updated successfully"
                };
                TempData["Success"] = message;

                // If moving to IN_TRANSIT, redirect to POD page
                if (newStatus == "IN_TRANSIT")
                {
                    return RedirectToAction("CurrentDelivery", new { id });
                }
            }
            else
            {
                TempData["Error"] = "Failed to update status";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating delivery {DeliveryId} status to {Status}", id, newStatus);
            TempData["Error"] = "Failed to update status";
        }

        return RedirectToAction("CurrentDelivery", new { id });
    }

    /// <summary>
    /// POD capture page
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "DP")]
    public async Task<IActionResult> Pod(Guid id)
    {
        var dpId = GetUserId();

        try
        {
            var delivery = await _deliveryService.GetDeliveryAsync(id);

            if (delivery == null)
            {
                TempData["Error"] = "Delivery not found";
                return RedirectToAction("Active");
            }

            if (delivery.Status != "IN_TRANSIT")
            {
                TempData["Error"] = "POD can only be captured when delivery is in transit";
                return RedirectToAction("CurrentDelivery", new { id });
            }

            var model = new PodCaptureViewModel
            {
                DeliveryId = delivery.Id,
                DropAddress = delivery.Drop.Address,
                RecipientName = delivery.Drop.ContactName,
                RecipientPhone = delivery.Drop.ContactPhone ?? ""
            };

            ViewData["Title"] = "Capture POD";
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading POD page for delivery {DeliveryId}", id);
            TempData["Error"] = "Failed to load POD page";
            return RedirectToAction("Active");
        }
    }

    /// <summary>
    /// Submit POD and complete delivery
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "DP")]
    public async Task<IActionResult> SubmitPod(PodCaptureViewModel model)
    {
        var dpId = GetUserId();

        try
        {
            // For now, simple validation - in production, verify OTP from a service
            if (model.PodType == "OTP" && string.IsNullOrEmpty(model.Otp))
            {
                ModelState.AddModelError("Otp", "Please enter the OTP");
                ViewData["Title"] = "Capture POD";
                return View("Pod", model);
            }

            if (model.PodType == "PHOTO" && string.IsNullOrEmpty(model.PhotoBase64))
            {
                ModelState.AddModelError("PhotoBase64", "Please capture a photo");
                ViewData["Title"] = "Capture POD";
                return View("Pod", model);
            }

            // Build metadata for the POD
            var metadata = System.Text.Json.JsonSerializer.Serialize(new
            {
                podType = model.PodType,
                recipientPresent = model.RecipientPresent,
                leftAtDoor = model.LeftAtDoor,
                notes = model.DeliveryNotes,
                photoNotes = model.PhotoNotes,
                timestamp = DateTime.UtcNow
            });

            var success = await _deliveryService.UpdateDeliveryStatusAsync(
                model.DeliveryId, "DELIVERED", dpId, "DP", metadata);

            if (success)
            {
                TempData["Success"] = "Delivery completed successfully! Great job!";
                return RedirectToAction("History");
            }
            else
            {
                TempData["Error"] = "Failed to complete delivery";
                return RedirectToAction("Pod", new { id = model.DeliveryId });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting POD for delivery {DeliveryId}", model.DeliveryId);
            TempData["Error"] = "Failed to submit POD";
            return RedirectToAction("Pod", new { id = model.DeliveryId });
        }
    }

    /// <summary>
    /// Send OTP to recipient (AJAX)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "DP")]
    public async Task<IActionResult> SendOtp(Guid deliveryId)
    {
        try
        {
            var delivery = await _deliveryService.GetDeliveryAsync(deliveryId);
            if (delivery == null)
            {
                return Json(new { success = false, message = "Delivery not found" });
            }

            // In production, integrate with SMS service
            // For now, simulate OTP sent
            var otp = new Random().Next(1000, 9999).ToString();

            _logger.LogInformation("OTP {Otp} sent to {Phone} for delivery {DeliveryId}",
                otp, delivery.Drop.ContactPhone, deliveryId);

            // Store OTP in session or cache (in production use proper OTP service)
            TempData[$"OTP_{deliveryId}"] = otp;

            return Json(new
            {
                success = true,
                message = $"OTP sent to {delivery.Drop.ContactPhone}",
                // In dev mode, return OTP for testing
                devOtp = otp
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending OTP for delivery {DeliveryId}", deliveryId);
            return Json(new { success = false, message = "Failed to send OTP" });
        }
    }

    /// <summary>
    /// Live tracking page for requester
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Track(Guid id)
    {
        try
        {
            var delivery = await _deliveryService.GetDeliveryAsync(id);

            if (delivery == null)
            {
                TempData["Error"] = "Delivery not found";
                return RedirectToAction("Index");
            }

            var model = new LiveTrackingViewModel
            {
                DeliveryId = delivery.Id,
                Status = delivery.Status,
                Pickup = new LocationViewModel
                {
                    Lat = delivery.Pickup.Lat,
                    Lng = delivery.Pickup.Lng,
                    Address = delivery.Pickup.Address
                },
                Drop = new LocationViewModel
                {
                    Lat = delivery.Drop.Lat,
                    Lng = delivery.Drop.Lng,
                    Address = delivery.Drop.Address
                },
                Timeline = delivery.Timeline.Select(t => new TimelineItemViewModel
                {
                    Status = t.Status,
                    Timestamp = t.Timestamp,
                    Description = t.Description
                }).ToList()
            };

            if (delivery.AssignedDP != null)
            {
                model.AssignedDP = new DPInfoViewModel
                {
                    DPId = delivery.AssignedDP.DPId,
                    Name = delivery.AssignedDP.DPName,
                    Phone = delivery.AssignedDP.DPPhone,
                    Rating = delivery.AssignedDP.Rating
                };
            }

            ViewData["Title"] = "Track Delivery";
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading tracking for delivery {DeliveryId}", id);
            TempData["Error"] = "Failed to load tracking";
            return RedirectToAction("Index");
        }
    }

    /// <summary>
    /// Get current DP location (AJAX for live tracking)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetLocation(Guid deliveryId)
    {
        try
        {
            var delivery = await _deliveryService.GetDeliveryAsync(deliveryId);
            if (delivery == null)
            {
                return Json(new { success = false });
            }

            // In production, get live DP location from tracking service
            // For now, simulate movement
            var progress = delivery.Status switch
            {
                "ACCEPTED" => 0.1,
                "PICKED_UP" => 0.3,
                "IN_TRANSIT" => 0.7,
                "DELIVERED" => 1.0,
                _ => 0
            };

            var currentLat = (double)delivery.Pickup.Lat +
                ((double)delivery.Drop.Lat - (double)delivery.Pickup.Lat) * progress;
            var currentLng = (double)delivery.Pickup.Lng +
                ((double)delivery.Drop.Lng - (double)delivery.Pickup.Lng) * progress;

            return Json(new
            {
                success = true,
                status = delivery.Status,
                lat = currentLat,
                lng = currentLng,
                lastUpdate = DateTime.UtcNow,
                estimatedMinutes = delivery.Status == "DELIVERED" ? 0 : (int)(20 * (1 - progress))
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting location for delivery {DeliveryId}", deliveryId);
            return Json(new { success = false });
        }
    }

    #endregion

    #region AJAX Endpoints

    /// <summary>
    /// Get matching candidates for a delivery (AJAX)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMatchingCandidates(Guid deliveryId)
    {
        try
        {
            var result = await _matchingService.MatchDeliveryAsync(deliveryId);

            return Json(new
            {
                success = result.IsSuccess,
                candidates = result.MatchedDPs.Select(dp => new
                {
                    dpId = dp.DPId,
                    name = dp.DPName,
                    rating = dp.Rating,
                    estimatedPrice = dp.EstimatedPrice,
                    distanceKm = dp.DistanceFromPickupKm
                }),
                totalMatches = result.TotalMatches,
                status = result.Status
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting matching candidates for delivery {DeliveryId}", deliveryId);
            return Json(new { success = false, error = "Failed to find candidates" });
        }
    }

    /// <summary>
    /// Calculate estimated price (AJAX)
    /// </summary>
    [HttpPost]
    public IActionResult CalculatePrice([FromBody] PriceCalculationRequest request)
    {
        try
        {
            var distanceKm = _serviceAreaService.CalculateDistanceKm(
                (double)request.PickupLat, (double)request.PickupLng,
                (double)request.DropLat, (double)request.DropLng);

            // Simple pricing: base + per km + per kg
            var baseRate = 30m;
            var perKmRate = 10m;
            var perKgRate = 2m;

            var estimatedPrice = baseRate + ((decimal)distanceKm * perKmRate) + (request.WeightKg * perKgRate);

            return Json(new
            {
                distanceKm = Math.Round(distanceKm, 2),
                estimatedPrice = Math.Round(estimatedPrice, 0)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating price");
            return Json(new { error = "Failed to calculate price" });
        }
    }

    #endregion
}

/// <summary>
/// Request model for price calculation
/// </summary>
public class PriceCalculationRequest
{
    public decimal PickupLat { get; set; }
    public decimal PickupLng { get; set; }
    public decimal DropLat { get; set; }
    public decimal DropLng { get; set; }
    public decimal WeightKg { get; set; }
}
