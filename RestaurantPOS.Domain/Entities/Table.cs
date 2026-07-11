using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Domain.Entities;

/// <summary>Dining table in a branch.</summary>
public class DiningTable : BaseEntity
{
    public Guid BranchId { get; set; }
    public Guid? SectionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public TableStatus Status { get; set; } = TableStatus.Available;
    public string? QrCode { get; set; }
    public string? Shape { get; set; } = "square";
    public int? PositionX { get; set; }
    public int? PositionY { get; set; }
    public bool IsActive { get; set; } = true;

    public Branch Branch { get; set; } = null!;
    public TableSection? Section { get; set; }
    public ICollection<Order> Orders { get; set; } = new HashSet<Order>();
    public ICollection<TableReservation> Reservations { get; set; } = new HashSet<TableReservation>();
}

/// <summary>Section/zone within a branch (e.g., Indoor, Outdoor, VIP).</summary>
public class TableSection : BaseEntity
{
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public Branch Branch { get; set; } = null!;
    public ICollection<DiningTable> Tables { get; set; } = new HashSet<DiningTable>();
}

/// <summary>Table reservation.</summary>
public class TableReservation : BaseEntity
{
    public Guid BranchId { get; set; }
    public Guid? TableId { get; set; }
    public Guid? CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public DateTime ReservationDate { get; set; }
    public TimeSpan ReservationTime { get; set; }
    public int Duration { get; set; } = 60;
    public int NumberOfGuests { get; set; } = 1;
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
    public string? Notes { get; set; }
    public string? SpecialRequests { get; set; }

    public Branch Branch { get; set; } = null!;
    public DiningTable? Table { get; set; }
    public Customer? Customer { get; set; }
}

/// <summary>Kitchen display system order.</summary>
public class KitchenOrder : BaseEntity
{
    public Guid OrderId { get; set; }
    public Guid BranchId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public KitchenOrderStatus Status { get; set; } = KitchenOrderStatus.New;
    public DateTime? AcknowledgedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Station { get; set; }
    public int? EstimatedMinutes { get; set; }
    public string? Notes { get; set; }

    public Order Order { get; set; } = null!;
    public Branch Branch { get; set; } = null!;
}
