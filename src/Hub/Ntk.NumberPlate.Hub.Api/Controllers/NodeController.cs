using Microsoft.AspNetCore.Mvc;
using Ntk.NumberPlate.Hub.Api.Models;
using Ntk.NumberPlate.Hub.Api.Services;
using Ntk.NumberPlate.Shared.Models;

namespace Ntk.NumberPlate.Hub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NodeController : ControllerBase
{
    private readonly INodeManagementService _nodeService;
    private readonly ILogger<NodeController> _logger;

    public NodeController(INodeManagementService nodeService, ILogger<NodeController> logger)
    {
        _nodeService = nodeService;
        _logger = logger;
    }

    /// <summary>
    /// ثبت نود جدید
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<string>>> RegisterNode([FromBody] NodeRegistrationModel model)
    {
        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _nodeService.RegisterNodeAsync(model.NodeId, model.NodeName, ipAddress);

            return Ok(ApiResponse<string>.SuccessResponse(model.NodeId, "نود با موفقیت ثبت شد"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در ثبت نود");
            return StatusCode(500, ApiResponse<string>.ErrorResponse("خطا در ثبت نود"));
        }
    }

    /// <summary>
    /// ارسال heartbeat
    /// </summary>
    [HttpPost("heartbeat/{nodeId}")]
    public async Task<ActionResult<ApiResponse<string>>> Heartbeat(string nodeId)
    {
        try
        {
            await _nodeService.UpdateHeartbeatAsync(nodeId);
            return Ok(ApiResponse<string>.SuccessResponse("OK"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در ثبت heartbeat");
            return StatusCode(500, ApiResponse<string>.ErrorResponse("خطا در ثبت heartbeat"));
        }
    }

    /// <summary>
    /// دریافت لیست تمام نودها
    /// </summary>
    [HttpGet("list")]
    public async Task<ActionResult<ApiResponse<List<NodeInfo>>>> GetAllNodes()
    {
        try
        {
            var nodes = await _nodeService.GetAllNodesAsync();
            return Ok(ApiResponse<List<NodeInfo>>.SuccessResponse(nodes));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت لیست نودها");
            return StatusCode(500, ApiResponse<List<NodeInfo>>.ErrorResponse("خطا در دریافت لیست نودها"));
        }
    }

    /// <summary>
    /// دریافت اطلاعات یک نود
    /// </summary>
    [HttpGet("{nodeId}")]
    public async Task<ActionResult<ApiResponse<NodeInfo>>> GetNode(string nodeId)
    {
        try
        {
            var node = await _nodeService.GetNodeAsync(nodeId);
            if (node == null)
                return NotFound(ApiResponse<NodeInfo>.ErrorResponse("نود یافت نشد"));

            return Ok(ApiResponse<NodeInfo>.SuccessResponse(node));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت اطلاعات نود");
            return StatusCode(500, ApiResponse<NodeInfo>.ErrorResponse("خطا در دریافت اطلاعات نود"));
        }
    }
}

public class NodeRegistrationModel
{
    public string NodeId { get; set; } = string.Empty;
    public string NodeName { get; set; } = string.Empty;
}


