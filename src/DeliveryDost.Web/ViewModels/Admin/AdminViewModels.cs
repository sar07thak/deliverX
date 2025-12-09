namespace DeliveryDost.Web.ViewModels.Admin;

public class UsersListViewModel
{
    public List<UserItemViewModel> Users { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; }
    public string? RoleFilter { get; set; }
    public string? StatusFilter { get; set; }
}

public class UserItemViewModel
{
    public Guid Id { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; }

    public string RoleBadgeClass => Role switch
    {
        "Admin" => "bg-danger",
        "DPCM" => "bg-purple",
        "DP" => "bg-warning text-dark",
        "BC" or "DBC" => "bg-info",
        "EC" => "bg-success",
        _ => "bg-secondary"
    };

    public string RoleDisplayName => Role switch
    {
        "Admin" => "Admin",
        "DPCM" => "Manager",
        "DP" => "Partner",
        "BC" or "DBC" => "Business",
        "EC" => "Consumer",
        _ => Role
    };
}

public class KycRequestsViewModel
{
    public List<KycRequestItemViewModel> Requests { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; }
    public string? StatusFilter { get; set; }
}

public class KycRequestItemViewModel
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserPhone { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string Status { get; set; } = "PENDING";
    public DateTime SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RejectionReason { get; set; }

    public string StatusBadgeClass => Status switch
    {
        "PENDING" => "bg-warning text-dark",
        "IN_PROGRESS" => "bg-info",
        "VERIFIED" => "bg-success",
        "APPROVED" => "bg-success",
        "REJECTED" => "bg-danger",
        _ => "bg-secondary"
    };
}

public class ComplaintsListViewModel
{
    public List<ComplaintItemViewModel> Complaints { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; }
    public string? StatusFilter { get; set; }
}

public class ComplaintItemViewModel
{
    public Guid Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Priority { get; set; } = "LOW";
    public string Status { get; set; } = "OPEN";
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CreatedByPhone { get; set; } = string.Empty;

    public string StatusBadgeClass => Status switch
    {
        "OPEN" => "bg-warning text-dark",
        "IN_PROGRESS" => "bg-info",
        "RESOLVED" => "bg-success",
        "CLOSED" => "bg-secondary",
        _ => "bg-secondary"
    };

    public string PriorityBadgeClass => Priority switch
    {
        "HIGH" => "bg-danger",
        "MEDIUM" => "bg-warning text-dark",
        "LOW" => "bg-info",
        _ => "bg-secondary"
    };
}
