using System.Text.Json;
using PharmacyApp.Models;

namespace PharmacyApp.Services;

/// <summary>Thread-safe local data store for medicines, sales, and event history used by the pharmacy app.</summary>
public class JsonDataStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private const int MaxEventsRetained = 500;

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _medicinesPath;
    private readonly string _salesPath;
    private readonly string _eventsPath;

    public JsonDataStore(IWebHostEnvironment env)
    {
        var dataDirectory = Path.Combine(env.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDirectory);
        _medicinesPath = Path.Combine(dataDirectory, "medicines.json");
        _salesPath = Path.Combine(dataDirectory, "sales.json");
        _eventsPath = Path.Combine(dataDirectory, "events.json");
    }

    public async Task<IReadOnlyList<Medicine>> GetMedicinesAsync(string? search = null)
    {
        await _gate.WaitAsync();
        try
        {
            var medicines = await ReadAsync<Medicine>(_medicinesPath);
            if (!string.IsNullOrWhiteSpace(search))
            {
                medicines = medicines
                    .Where(m => m.Name.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return medicines.OrderBy(m => m.Name).ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<Medicine?> GetMedicineAsync(int id)
    {
        await _gate.WaitAsync();
        try
        {
            var medicines = await ReadAsync<Medicine>(_medicinesPath);
            return medicines.FirstOrDefault(m => m.Id == id);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<Medicine> AddMedicineAsync(Medicine medicine)
    {
        await _gate.WaitAsync();
        try
        {
            var medicines = await ReadAsync<Medicine>(_medicinesPath);
            medicine.Id = medicines.Count == 0 ? 1 : medicines.Max(m => m.Id) + 1;
            medicines.Add(medicine);
            await WriteAsync(_medicinesPath, medicines);
            await AppendEventAsync(
                "MedicineAdded",
                $"Added {medicine.Name} ({medicine.Brand}), qty {medicine.Quantity} at {medicine.Price:0.00}.",
                "Medicine",
                medicine.Id);
            return medicine;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<Sale>> GetSalesAsync()
    {
        await _gate.WaitAsync();
        try
        {
            var sales = await ReadAsync<Sale>(_salesPath);
            return sales.OrderByDescending(s => s.SoldOn).ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Records a sale and decrements stock atomically. Returns null when the medicine is unknown.</summary>
    public async Task<(Sale? Sale, string? Error)> RecordSaleAsync(SaleRequest request)
    {
        await _gate.WaitAsync();
        try
        {
            var medicines = await ReadAsync<Medicine>(_medicinesPath);
            var medicine = medicines.FirstOrDefault(m => m.Id == request.MedicineId);
            if (medicine is null)
            {
                await AppendEventAsync("SaleRejected", $"Sale rejected: medicine {request.MedicineId} not found.", "Medicine", request.MedicineId, "Warning");
                return (null, "Medicine not found.");
            }

            if (medicine.Quantity < request.Quantity)
            {
                await AppendEventAsync(
                    "SaleRejected",
                    $"Sale rejected for {medicine.Name}: requested {request.Quantity}, available {medicine.Quantity}.",
                    "Medicine",
                    medicine.Id,
                    "Warning");
                return (null, $"Only {medicine.Quantity} unit(s) of {medicine.Name} in stock.");
            }

            medicine.Quantity -= request.Quantity;

            var sales = await ReadAsync<Sale>(_salesPath);
            var sale = new Sale
            {
                Id = sales.Count == 0 ? 1 : sales.Max(s => s.Id) + 1,
                MedicineId = medicine.Id,
                MedicineName = medicine.Name,
                Quantity = request.Quantity,
                UnitPrice = medicine.Price,
                TotalAmount = Math.Round(medicine.Price * request.Quantity, 2),
                SoldOn = DateTime.UtcNow,
                CustomerName = string.IsNullOrWhiteSpace(request.CustomerName) ? null : request.CustomerName.Trim()
            };
            sales.Add(sale);

            await WriteAsync(_salesPath, sales);
            await WriteAsync(_medicinesPath, medicines);
            await AppendEventAsync(
                "SaleRecorded",
                $"Sold {sale.Quantity} x {sale.MedicineName} for {sale.TotalAmount:0.00}. Remaining stock {medicine.Quantity}.",
                "Sale",
                sale.Id);
            return (sale, null);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<EventLogEntry>> GetEventsAsync(int take = 100)
    {
        await _gate.WaitAsync();
        try
        {
            var events = await ReadAsync<EventLogEntry>(_eventsPath);
            return events.OrderByDescending(e => e.TimestampUtc).Take(Math.Clamp(take, 1, MaxEventsRetained)).ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Appends an audit entry. Callers must already hold <see cref="_gate"/>.</summary>
    private async Task AppendEventAsync(string eventType, string message, string? entityType, int? entityId, string level = "Information")
    {
        var events = await ReadAsync<EventLogEntry>(_eventsPath);
        events.Add(new EventLogEntry
        {
            Id = events.Count == 0 ? 1 : events.Max(e => e.Id) + 1,
            TimestampUtc = DateTime.UtcNow,
            Level = level,
            EventType = eventType,
            Message = message,
            EntityType = entityType,
            EntityId = entityId
        });

        if (events.Count > MaxEventsRetained)
        {
            events = events.Skip(events.Count - MaxEventsRetained).ToList();
        }

        await WriteAsync(_eventsPath, events);
    }

    private static async Task<List<T>> ReadAsync<T>(string path)
    {
        if (!File.Exists(path))
        {
            return [];
        }

        await using var stream = File.OpenRead(path);
        if (stream.Length == 0)
        {
            return [];
        }

        return await JsonSerializer.DeserializeAsync<List<T>>(stream, SerializerOptions) ?? [];
    }

    private static async Task WriteAsync<T>(string path, List<T> items)
    {
        var temporaryPath = path + ".tmp";
        await using (var stream = File.Create(temporaryPath))
        {
            await JsonSerializer.SerializeAsync(stream, items, SerializerOptions);
        }

        File.Move(temporaryPath, path, overwrite: true);
    }
}
