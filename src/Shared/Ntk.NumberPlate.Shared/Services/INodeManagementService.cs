using Ntk.NumberPlate.Shared.Models;

namespace Ntk.NumberPlate.Shared.Services;

public interface INodeManagementService
{
    Task RegisterNodeAsync(string nodeId, string nodeName, string? ipAddress);
    Task UpdateHeartbeatAsync(string nodeId);
    Task<List<NodeInfo>> GetAllNodesAsync();
    Task<NodeInfo?> GetNodeAsync(string nodeId);
}
