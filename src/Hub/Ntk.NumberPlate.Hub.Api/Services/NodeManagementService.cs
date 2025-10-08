using Microsoft.EntityFrameworkCore;
using Ntk.NumberPlate.Hub.Api.Data;
using Ntk.NumberPlate.Hub.Api.Models;

namespace Ntk.NumberPlate.Hub.Api.Services;

public class NodeManagementService : INodeManagementService
{
    private readonly HubDbContext _context;
    private readonly ILogger<NodeManagementService> _logger;

    public NodeManagementService(HubDbContext context, ILogger<NodeManagementService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task RegisterNodeAsync(string nodeId, string nodeName, string? ipAddress)
    {
        var node = await _context.Nodes.FindAsync(nodeId);

        if (node == null)
        {
            node = new NodeInfo
            {
                NodeId = nodeId,
                NodeName = nodeName,
                IpAddress = ipAddress,
                IsOnline = true,
                LastHeartbeat = DateTime.UtcNow
            };
            _context.Nodes.Add(node);
            _logger.LogInformation($"نود جدید ثبت شد: {nodeName} ({nodeId})");
        }
        else
        {
            node.NodeName = nodeName;
            node.IpAddress = ipAddress;
            node.IsOnline = true;
            node.LastHeartbeat = DateTime.UtcNow;
            _logger.LogInformation($"نود به‌روزرسانی شد: {nodeName} ({nodeId})");
        }

        await _context.SaveChangesAsync();
    }

    public async Task UpdateHeartbeatAsync(string nodeId)
    {
        var node = await _context.Nodes.FindAsync(nodeId);
        if (node != null)
        {
            node.LastHeartbeat = DateTime.UtcNow;
            node.IsOnline = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<NodeInfo>> GetAllNodesAsync()
    {
        var nodes = await _context.Nodes.ToListAsync();

        // به‌روزرسانی وضعیت آنلاین بودن
        foreach (var node in nodes)
        {
            node.IsOnline = (DateTime.UtcNow - node.LastHeartbeat).TotalMinutes < 5;
        }

        await _context.SaveChangesAsync();

        return nodes;
    }

    public async Task<NodeInfo?> GetNodeAsync(string nodeId)
    {
        return await _context.Nodes.FindAsync(nodeId);
    }
}


