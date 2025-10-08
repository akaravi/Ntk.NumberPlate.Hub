using Ntk.NumberPlate.Hub.Api.Models;

namespace Ntk.NumberPlate.Hub.Api.Services;

public interface INodeManagementService
{
    Task RegisterNodeAsync(string nodeId, string nodeName, string? ipAddress);
    Task UpdateHeartbeatAsync(string nodeId);
    Task<List<NodeInfo>> GetAllNodesAsync();
    Task<NodeInfo?> GetNodeAsync(string nodeId);
}


