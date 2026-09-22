using System.ComponentModel.DataAnnotations;

namespace PharmacyApp.Models;

public class Medicine
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Notes { get; set; }

    [Required]
    public DateOnly ExpiryDate { get; set; }

    [Range(0, int.MaxValue)]
    public int Quantity { get; set; }

    [Range(0, 1_000_000)]
    public decimal Price { get; set; }

    [Required, StringLength(120)]
    public string Brand { get; set; } = string.Empty;
}
