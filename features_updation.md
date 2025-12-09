# DeliveryDost Features Implementation Tracker

## Implementation Status Legend
- [x] Completed
- [ ] Pending
- [~] Partially Done

---

## GROUP 1: Pincode Master & Super Admin Reports (COMPLETED)

### Pincode Master API
- [x] In entire project for state, city id use the masters or pincode api.
  - **Implemented**: `PincodeMaster`, `StateMaster`, `DistrictMaster` entities
  - **API Endpoints**: `/api/Master/pincode/{pincode}`, `/api/Master/states`, `/api/Master/districts/{stateCode}`
  - **Service**: `IPincodeService`, `PincodeService`

### Super Admin Dashboard Reports

#### Report of EndConsumer [No Aadhar Verification Required] - [x] COMPLETED
- [x] Name
- [x] Mobile Number [Encrypted and view on hover]
- [x] Email Id [Encrypted and view on hover]
- [x] State
- [x] District
- [x] Pincode
- [x] Address
- [x] Date Of Birth
- [x] Date of Joining
- [x] Status
- [x] Last Services Access Date
- **View**: `Views/Admin/EndConsumerReport.cshtml`

#### Report of BusinessConsumer - [x] COMPLETED
- [x] Name
- [x] Business Name (If any)
- [x] Personal PAN [with verification status] - [Encrypted and view on hover]
- [x] Business PAN (If any) [with verification status] - [Encrypted and view on hover]
- [x] Aadhar [with verification status] - [Encrypted and view on hover]
- [x] Mobile Number [Encrypted and view on hover]
- [x] Email Id [Encrypted and view on hover]
- [x] State
- [x] District
- [x] Pincode
- [x] Address
- [x] Date Of Birth
- [x] Date of Joining
- [x] Number of Pickup Locations
- [x] Status
- [x] Last Services Access Date
- **View**: `Views/Admin/BusinessConsumerReport.cshtml`

#### Report of DeliveryPartners - [x] COMPLETED
- [x] Name
- [x] Personal PAN [with verification status] - [Encrypted and view on hover]
- [x] Aadhar [with verification status] - [Encrypted and view on hover]
- [x] Mobile Number [Encrypted and view on hover]
- [x] Email Id [Encrypted and view on hover]
- [x] State
- [x] District
- [x] Pincode
- [x] Address
- [x] Date Of Birth
- [x] Date of Joining
- [x] Service Area
- [x] Delivery Rate per kg per km basis
- [x] Status
- [x] Last Services Access Date
- **View**: `Views/Admin/DeliveryPartnerReport.cshtml`

#### Report of DPCM - [x] COMPLETED
- [x] Name
- [x] Business Name (If any)
- [x] Personal PAN [with verification status] - [Encrypted and view on hover]
- [x] Business PAN (If any) [with verification status] - [Encrypted and view on hover]
- [x] Aadhar [with verification status] - [Encrypted and view on hover]
- [x] Mobile Number [Encrypted and view on hover]
- [x] Email Id [Encrypted and view on hover]
- [x] State
- [x] District
- [x] Pincode
- [x] Address
- [x] Date Of Birth
- [x] Date of Joining
- [x] Number of Pickup Locations
- [x] Status
- [x] Last Services Access Date
- [x] Number of business user in his area
- [x] Number of End User in his area
- [x] Number of Delivery Partner in his area
- **View**: `Views/Admin/DPCMReport.cshtml`

---

## GROUP 2: DPCM Management & BC Registration (COMPLETED)

### DPCM (Delivery Partner Cluster Manager) Features

#### 1. DPCM Manual Registration with Security Deposit - [x] COMPLETED
- [x] DPCM would not be allowed to onboard themselves
- [x] Security deposit input at time of registration
- [x] Manual registration process by SuperAdmin
- [x] Agreement document upload section
- **Entity Fields Added**: `SecurityDeposit`, `SecurityDepositStatus`, `SecurityDepositReceivedAt`, `SecurityDepositTransactionRef`, `AgreementDocumentUrl`, `AgreementSignedAt`, `AgreementVersion`
- **Service**: `IDPCMManagementService`, `DPCMManagementService`
- **DTOs**: `CreateDPCMRequest`, `CreateDPCMResponse`, `UpdateDPCMRequest`

#### 2. DPCM Pincode-Area Mapping - [x] COMPLETED
- [x] DPCM's working area mapped as per pincode wise
- [x] One Pincode – One DPCM (unique constraint enforced)
- **Entity**: `PincodeDPCMMapping` with filtered unique index
- **Methods**: `AssignPincodesToDPCMAsync`, `UnassignPincodesAsync`, `GetDPCMByPincodeAsync`, `GetDPCMPincodesAsync`

#### 3. DPCM Revenue Configuration - [x] COMPLETED
- [x] Fees shared per completed delivery [Amount or % whichever is higher]
- [x] Payment including GST (18%)
- [x] HYBRID commission type: `Max(PercentageAmount, MinAmount)`
- **Entity Fields**: `CommissionType` (PERCENTAGE/FLAT/HYBRID), `CommissionValue`, `MinCommissionAmount`
- **Method**: `CalculateCommissionAsync` with GST calculation
- [ ] Long-term: Quality & complaint adjustments (noted for future)

#### 5. Delivery Partner Revenue - [~] PARTIALLY NOTED
- [x] Fees shared per completed delivery as per bid (existing `DPPricingConfig`)
- [x] Bid cannot exceed registration price (validation in `BiddingService`)
- [ ] Long-term: Quality & complaint adjustments (noted for future)

### Business Consumer Features

#### Business Consumer Registration with Subscription - [x] COMPLETED
- [x] Business constitution dropdown (Proprietorship, Partnership, LLP, Private Limited, etc.)
- [x] GST Registration Type dropdown [Regular/Composition/Unregistered]
- [x] Bank Details section in registration
- [x] Subscription Plan selection (mandatory at onboarding)
- [x] Pick-up location page
- [x] Platform fix fee subscription
- **Entity Fields Added**: `BusinessConstitution`, `GSTRegistrationType`, `SubscriptionStartDate`, `PickupLocationsJson`
- **New Entity**: `BCPickupLocation`
- **Controller**: `BcController` (4-step registration)
- **View Models**: `BcRegistrationViewModels.cs`

---

## GROUP 3: End Consumer Delivery Features (COMPLETED)

### On Create Delivery Page

#### a. Add Address Name field in Pickup & Drop Location - [x] COMPLETED
- [x] Address Name/Label field (e.g., "Home", "Office", "Warehouse A")
- **Entity Fields Added**: `PickupAddressName`, `DropAddressName` in `Delivery` entity
- **ViewModel Fields**: `PickupAddressName`, `DropAddressName` in `CreateDeliveryViewModel`

#### b. Add checkbox (Want to permanently Save) - [x] COMPLETED
- [x] Save address checkbox for both pickup and drop locations
- **ViewModel Fields**: `SavePickupAddress`, `SaveDropAddress`

#### c. Saved addresses reusable for next time - [x] COMPLETED
- [x] Full saved address management system
- **New Entity**: `SavedAddress` with full contact details
- **Configuration**: `SavedAddressConfiguration.cs`
- **Service**: `ISavedAddressService`, `SavedAddressService`
- **DTOs**: `CreateSavedAddressRequest`, `UpdateSavedAddressRequest`, `SavedAddressDto`, `SavedAddressListDto`
- **ViewModel Fields**: `SavedPickupAddresses`, `SavedDropAddresses`, `PickupSavedAddressId`, `DropSavedAddressId`

#### d. Latlong selection via map pin - [x] COMPLETED
- [x] Latitude/Longitude fields with map integration support
- **Existing Fields**: `PickupLat`, `PickupLng`, `DropLat`, `DropLng` with validation
- **ViewModel**: Added range validation for coordinates

#### e. Alternate contact number, mail id & WhatsApp number fields - [x] COMPLETED
- [x] Alternate phone for pickup and drop
- [x] Email for pickup and drop
- [x] WhatsApp number for pickup and drop
- **Entity Fields Added**:
  - `PickupAlternatePhone`, `PickupContactEmail`, `PickupWhatsAppNumber`
  - `DropAlternatePhone`, `DropContactEmail`, `DropWhatsAppNumber`
- **ViewModel Fields**: Same fields added with validation

#### f. Contact number validation (Indian phone: starts with 6-9, 10 digits) - [x] COMPLETED
- [x] Regex validation: `^[6-9]\d{9}$`
- **Applied To**: `PickupContactPhone`, `PickupAlternatePhone`, `PickupWhatsAppNumber`, `DropContactPhone`, `DropAlternatePhone`, `DropWhatsAppNumber`

#### g. Google Distance Calculator using latlong - [x] COMPLETED
- [x] Google Distance Matrix API integration
- [x] Haversine formula fallback
- [x] Route polyline support via Google Directions API
- **Service**: `IDistanceCalculatorService`, `DistanceCalculatorService`
- **DTOs**: `DistanceCalculationResult`, `RouteCalculationResult`, `LatLngPoint`
- **Entity Fields Added**: `DistanceSource`, `RoutePolyline` in `Delivery`
- **Config Key**: `Google:MapsApiKey` in appsettings.json

#### h. Weight field for weighing purpose - [x] EXISTING
- [x] Weight field already exists: `WeightKg` (0.1 to 100 kg)

#### i. Caution field for hazardous items - [x] COMPLETED
- [x] Hazardous item flag
- [x] Caution type dropdown (FRAGILE, FLAMMABLE, PERISHABLE, LIQUID, GLASS, ELECTRONIC, CHEMICAL, etc.)
- [x] Caution notes field
- [x] Special handling requirement flag
- **Entity Fields Added**: `IsHazardous`, `CautionType`, `CautionNotes`, `RequiresSpecialHandling`
- **ViewModel Options**: `CautionTypes` dropdown list

---

## GROUP 4: Delivery Partner Features (COMPLETED)

### Delivery Partner Enhancements

#### Select Service area in radius of XYZ KM from ABC latlong - [x] COMPLETED
- [x] Service area center point (lat/lng)
- [x] Configurable radius in km
- [x] Circle boundary generation for map display
- [x] Service area polygon support (for complex areas)
- [x] Pincode-based service area support
- **Entity Fields Added**:
  - `ServiceAreaCenterLat`, `ServiceAreaCenterLng`, `ServiceAreaRadiusKm` (enhanced)
  - `ServiceAreaPolygonJson`, `ServiceAreaPincodesJson`
- **Service Methods**: `SetServiceAreaAsync`, `GetServiceAreaAsync`
- **DTOs**: `SetServiceAreaRequest`, `ServiceAreaDto`, `LatLngDto`

#### Rate per kg per km - [x] EXISTING
- [x] Already exists in `DPPricingConfig`: `PerKmRate`, `PerKgRate`, `MinCharge`

#### Proper bidding platform on Available Jobs page - [x] COMPLETED
- [x] Full bidding system implementation
- [x] Place, withdraw, accept, reject bids
- [x] Bid validation (min/max amounts, expiry)
- [x] Bid cannot exceed registration price
- [x] Bid history and statistics
- **New Entity**: `DeliveryBid` with status tracking (PENDING, ACCEPTED, REJECTED, WITHDRAWN, EXPIRED)
- **New Entity**: `BiddingConfig` for platform-wide bidding rules
- **Configuration**: `DeliveryBidConfiguration.cs`, `BiddingConfigConfiguration.cs`
- **Service**: `IBiddingService`, `BiddingService`
- **DTOs**:
  - `PlaceBidRequest`, `PlaceBidResponse`
  - `AcceptBidRequest`, `RejectBidRequest`
  - `BidDto`, `DeliveryBidsResponse`
  - `AvailableDeliveryForBidDto`, `DPBidHistoryDto`, `BidHistoryItemDto`

#### One-direction deliveries restriction (at one time) - [x] COMPLETED
- [x] Direction preference setting (NORTH, SOUTH, EAST, WEST, ANY)
- [x] One-direction only flag
- [x] Direction angle calculation using bearing
- [x] Delivery filtering based on direction preference
- **Entity Fields Added**: `OneDirectionOnly`, `PreferredDirection`, `DirectionAngleDegrees`
- **Service Methods**: `CheckDirectionMatchAsync`, bearing calculation helpers

---

## Database Migrations Applied
1. `AddPincodeMasterTables` - PincodeMasters, StateMasters, DistrictMasters
2. `AddGroup2Features` - DPCManager fields, PincodeDPCMMappings, BCPickupLocations, BusinessConsumerProfile fields
3. `AddGroup3Features` - SavedAddresses, Delivery enhancements (address name, alternate contacts, caution)
4. `AddGroup4Features` - DeliveryBids, BiddingConfigs, DeliveryPartnerProfile enhancements

## Files Created/Modified

### Group 1 Files:
- `src/DeliveryDost.Domain/Entities/PincodeMaster.cs`
- `src/DeliveryDost.Infrastructure/Data/Configurations/PincodeMasterConfiguration.cs`
- `src/DeliveryDost.Application/DTOs/Master/PincodeDTOs.cs`
- `src/DeliveryDost.Application/DTOs/Reports/SuperAdminReportDTOs.cs`
- `src/DeliveryDost.Application/Services/IPincodeService.cs`
- `src/DeliveryDost.Application/Services/ISuperAdminReportService.cs`
- `src/DeliveryDost.Infrastructure/Services/PincodeService.cs`
- `src/DeliveryDost.Infrastructure/Services/SuperAdminReportService.cs`
- `src/DeliveryDost.Web/Controllers/MasterController.cs`
- `src/DeliveryDost.Web/Controllers/AdminController.cs` (updated)
- `src/DeliveryDost.Web/ViewModels/Admin/ReportViewModels.cs`
- `src/DeliveryDost.Web/Views/Admin/EndConsumerReport.cshtml`
- `src/DeliveryDost.Web/Views/Admin/BusinessConsumerReport.cshtml`
- `src/DeliveryDost.Web/Views/Admin/DeliveryPartnerReport.cshtml`
- `src/DeliveryDost.Web/Views/Admin/DPCMReport.cshtml`

### Group 2 Files:
- `src/DeliveryDost.Domain/Entities/DPCManager.cs` (updated with security deposit, agreement, pincode mappings)
- `src/DeliveryDost.Domain/Entities/BusinessConsumerProfile.cs` (updated with constitution, GST type, pickup locations)
- `src/DeliveryDost.Infrastructure/Data/Configurations/DPCManagerConfiguration.cs` (updated)
- `src/DeliveryDost.Infrastructure/Data/Configurations/BusinessConsumerProfileConfiguration.cs` (updated)
- `src/DeliveryDost.Application/DTOs/DPCM/DPCMManagementDTOs.cs`
- `src/DeliveryDost.Application/Services/IDPCMManagementService.cs`
- `src/DeliveryDost.Infrastructure/Services/DPCMManagementService.cs`
- `src/DeliveryDost.Web/Controllers/BcController.cs`
- `src/DeliveryDost.Web/ViewModels/Bc/BcRegistrationViewModels.cs`
- `src/DeliveryDost.Web/Controllers/AccountController.cs` (updated for BC redirect)

### Group 3 Files:
- `src/DeliveryDost.Domain/Entities/SavedAddress.cs` (NEW)
- `src/DeliveryDost.Domain/Entities/Delivery.cs` (updated with address name, alternate contacts, caution)
- `src/DeliveryDost.Infrastructure/Data/Configurations/SavedAddressConfiguration.cs` (NEW)
- `src/DeliveryDost.Infrastructure/Data/Configurations/DeliveryConfiguration.cs` (updated)
- `src/DeliveryDost.Infrastructure/Data/ApplicationDbContext.cs` (updated - SavedAddresses DbSet)
- `src/DeliveryDost.Application/DTOs/SavedAddress/SavedAddressDTOs.cs` (NEW)
- `src/DeliveryDost.Application/DTOs/Delivery/CreateDeliveryRequest.cs` (updated with alternate contacts, caution)
- `src/DeliveryDost.Application/Services/ISavedAddressService.cs` (NEW)
- `src/DeliveryDost.Application/Services/IDistanceCalculatorService.cs` (NEW)
- `src/DeliveryDost.Infrastructure/Services/SavedAddressService.cs` (NEW)
- `src/DeliveryDost.Infrastructure/Services/DistanceCalculatorService.cs` (NEW)
- `src/DeliveryDost.Web/ViewModels/Delivery/DeliveryViewModels.cs` (updated with all Group 3 fields)

### Group 4 Files:
- `src/DeliveryDost.Domain/Entities/DeliveryBid.cs` (NEW - includes BiddingConfig)
- `src/DeliveryDost.Domain/Entities/DeliveryPartnerProfile.cs` (updated with service area radius, direction preference)
- `src/DeliveryDost.Infrastructure/Data/Configurations/DeliveryBidConfiguration.cs` (NEW)
- `src/DeliveryDost.Infrastructure/Data/ApplicationDbContext.cs` (updated - DeliveryBids, BiddingConfigs DbSets)
- `src/DeliveryDost.Application/DTOs/Bidding/BiddingDTOs.cs` (NEW)
- `src/DeliveryDost.Application/Services/IBiddingService.cs` (NEW)
- `src/DeliveryDost.Infrastructure/Services/BiddingService.cs` (NEW)

---

## Summary

| Group | Features | Status |
|-------|----------|--------|
| Group 1 | Pincode API + 4 Super Admin Reports | COMPLETED |
| Group 2 | DPCM Management + BC Registration | COMPLETED |
| Group 3 | End Consumer Delivery Enhancements | COMPLETED |
| Group 4 | Delivery Partner Enhancements | COMPLETED |

**Last Updated**: December 8, 2025

---

## Next Steps (Future Enhancements)

1. **Quality & Complaint Adjustments** - Integrate quality scores and complaint history into revenue calculations
2. **Weight Verification** - Implement actual weight measurement integration
3. **Real-time Tracking** - Enhanced GPS tracking with location history
4. **Payment Gateway Integration** - Wallet top-up and payment processing
5. **Push Notifications** - Real-time notifications for bids, status updates
