using Microsoft.EntityFrameworkCore;
using Ntk.NumberPlate.Hub.Api.Data;
using Ntk.NumberPlate.Hub.Api.Models;
using Ntk.NumberPlate.Shared.Models;

namespace Ntk.NumberPlate.Hub.Api.Services;

public class VehicleDetectionService : IVehicleDetectionService
{
    private readonly HubDbContext _context;
    private readonly ILogger<VehicleDetectionService> _logger;

    public VehicleDetectionService(HubDbContext context, ILogger<VehicleDetectionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Guid> SaveDetectionAsync(VehicleDetectionData detection, string imagePath)
    {
        var record = new VehicleDetectionRecord
        {
            Id = detection.Id,
            NodeId = detection.NodeId,
            NodeName = detection.NodeName,
            PlateNumber = detection.PlateNumber,
            Speed = detection.Speed,
            DetectionTime = detection.DetectionTime,
            ImagePath = imagePath,
            Confidence = detection.Confidence,
            VehicleType = detection.VehicleType,
            VehicleColor = detection.VehicleColor,
            IsSpeedViolation = detection.IsSpeedViolation,
            SpeedLimit = detection.SpeedLimit
        };

        _context.VehicleDetections.Add(record);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"ذخیره تشخیص جدید: {detection.PlateNumber} از نود {detection.NodeName}");

        return record.Id;
    }

    public async Task<List<VehicleDetectionData>> GetRecentDetectionsAsync(int count = 100)
    {
        var records = await _context.VehicleDetections
            .OrderByDescending(x => x.DetectionTime)
            .Take(count)
            .ToListAsync();

        return records.Select(MapToDetectionData).ToList();
    }

    public async Task<List<VehicleDetectionData>> GetDetectionsByPlateAsync(string plateNumber)
    {
        var records = await _context.VehicleDetections
            .Where(x => x.PlateNumber == plateNumber)
            .OrderByDescending(x => x.DetectionTime)
            .ToListAsync();

        return records.Select(MapToDetectionData).ToList();
    }

    public async Task<List<VehicleDetectionData>> GetDetectionsByNodeAsync(string nodeId, DateTime? from = null, DateTime? to = null)
    {
        var query = _context.VehicleDetections.Where(x => x.NodeId == nodeId);

        if (from.HasValue)
            query = query.Where(x => x.DetectionTime >= from.Value);

        if (to.HasValue)
            query = query.Where(x => x.DetectionTime <= to.Value);

        var records = await query.OrderByDescending(x => x.DetectionTime).ToListAsync();

        return records.Select(MapToDetectionData).ToList();
    }

    public async Task<Dictionary<string, int>> GetStatisticsAsync()
    {
        var totalDetections = await _context.VehicleDetections.CountAsync();
        var todayDetections = await _context.VehicleDetections
            .Where(x => x.DetectionTime.Date == DateTime.Today)
            .CountAsync();
        var speedViolations = await _context.VehicleDetections
            .Where(x => x.IsSpeedViolation)
            .CountAsync();
        var activeNodes = await _context.Nodes
            .Where(x => x.IsOnline)
            .CountAsync();

        return new Dictionary<string, int>
        {
            { "TotalDetections", totalDetections },
            { "TodayDetections", todayDetections },
            { "SpeedViolations", speedViolations },
            { "ActiveNodes", activeNodes }
        };
    }

    private VehicleDetectionData MapToDetectionData(VehicleDetectionRecord record)
    {
        return new VehicleDetectionData
        {
            Id = record.Id,
            NodeId = record.NodeId,
            NodeName = record.NodeName,
            PlateNumber = record.PlateNumber,
            Speed = record.Speed,
            DetectionTime = record.DetectionTime,
            ImageFileName = Path.GetFileName(record.ImagePath),
            Confidence = record.Confidence,
            VehicleType = record.VehicleType,
            VehicleColor = record.VehicleColor,
            IsSpeedViolation = record.IsSpeedViolation,
            SpeedLimit = record.SpeedLimit
        };
    }
}


