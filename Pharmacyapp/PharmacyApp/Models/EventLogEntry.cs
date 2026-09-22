namespace PharmacyApp.Models;

public class EventLogEntry
{
    public int Id { get; set; }

    public DateTime TimestampUtc { get; set; }

    public string Level { get; set; } = "Information";

    /// <summary>Event name such as MedicineAdded or SaleRecorded.</summary>
    public string EventType { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? EntityType { get; set; }

    public int? EntityId { get; set; }
}
