using System.ComponentModel.DataAnnotations;

namespace PharmacyApp.Models;

public class Sale
{
    public int Id { get; set; }

    public int MedicineId { get; set; }

    public string MedicineName { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalAmount { get; set; }

    public DateTime SoldOn { get; set; }

    [StringLength(120)]
    public string? CustomerName { get; set; }
}

public class SaleRequest
{
    [Range(1, int.MaxValue)]
    public int MedicineId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [StringLength(120)]
    public string? CustomerName { get; set; }
}
