using Ntk.NumberPlate.Shared.Models;

namespace Ntk.NumberPlate.Hub.Api.Services;

public interface IVehicleDetectionService
{
    Task<Guid> SaveDetectionAsync(VehicleDetectionData detection, string imagePath);
    Task<List<VehicleDetectionData>> GetRecentDetectionsAsync(int count = 100);
    Task<List<VehicleDetectionData>> GetDetectionsByPlateAsync(string plateNumber);
    Task<List<VehicleDetectionData>> GetDetectionsByNodeAsync(string nodeId, DateTime? from = null, DateTime? to = null);
    Task<Dictionary<string, int>> GetStatisticsAsync();
}


