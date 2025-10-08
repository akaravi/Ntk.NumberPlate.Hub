using Microsoft.AspNetCore.Mvc;
using Ntk.NumberPlate.Hub.Api.Services;
using Ntk.NumberPlate.Shared.Models;

namespace Ntk.NumberPlate.Hub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VehicleDetectionController : ControllerBase
{
    private readonly IVehicleDetectionService _detectionService;
    private readonly INodeManagementService _nodeService;
    private readonly IImageStorageService _imageStorage;
    private readonly ILogger<VehicleDetectionController> _logger;

    public VehicleDetectionController(
        IVehicleDetectionService detectionService,
        INodeManagementService nodeService,
        IImageStorageService imageStorage,
        ILogger<VehicleDetectionController> logger)
    {
        _detectionService = detectionService;
        _nodeService = nodeService;
        _imageStorage = imageStorage;
        _logger = logger;
    }

    /// <summary>
    /// دریافت تشخیص جدید از نود
    /// </summary>
    [HttpPost("submit")]
    public async Task<ActionResult<ApiResponse<Guid>>> SubmitDetection([FromForm] VehicleDetectionSubmitModel model)
    {
        try
        {
            // ثبت heartbeat نود
            await _nodeService.UpdateHeartbeatAsync(model.Detection.NodeId);

            // ذخیره تصویر
            string imagePath = string.Empty;
            if (model.Image != null && model.Image.Length > 0)
            {
                using var memoryStream = new MemoryStream();
                await model.Image.CopyToAsync(memoryStream);
                var imageData = memoryStream.ToArray();

                var fileName = $"{model.Detection.Id}_{Path.GetFileName(model.Image.FileName)}";
                imagePath = await _imageStorage.SaveImageAsync(imageData, fileName);
            }

            // ذخیره اطلاعات تشخیص
            var detectionId = await _detectionService.SaveDetectionAsync(model.Detection, imagePath);

            return Ok(ApiResponse<Guid>.SuccessResponse(detectionId, "تشخیص با موفقیت ثبت شد"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در ثبت تشخیص");
            return StatusCode(500, ApiResponse<Guid>.ErrorResponse("خطا در ثبت تشخیص"));
        }
    }

    /// <summary>
    /// دریافت آخرین تشخیص‌ها
    /// </summary>
    [HttpGet("recent")]
    public async Task<ActionResult<ApiResponse<List<VehicleDetectionData>>>> GetRecentDetections([FromQuery] int count = 100)
    {
        try
        {
            var detections = await _detectionService.GetRecentDetectionsAsync(count);
            return Ok(ApiResponse<List<VehicleDetectionData>>.SuccessResponse(detections));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت تشخیص‌ها");
            return StatusCode(500, ApiResponse<List<VehicleDetectionData>>.ErrorResponse("خطا در دریافت تشخیص‌ها"));
        }
    }

    /// <summary>
    /// جستجوی تشخیص‌ها بر اساس پلاک
    /// </summary>
    [HttpGet("by-plate/{plateNumber}")]
    public async Task<ActionResult<ApiResponse<List<VehicleDetectionData>>>> GetByPlate(string plateNumber)
    {
        try
        {
            var detections = await _detectionService.GetDetectionsByPlateAsync(plateNumber);
            return Ok(ApiResponse<List<VehicleDetectionData>>.SuccessResponse(detections));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در جستجوی پلاک");
            return StatusCode(500, ApiResponse<List<VehicleDetectionData>>.ErrorResponse("خطا در جستجوی پلاک"));
        }
    }

    /// <summary>
    /// دریافت تشخیص‌های یک نود
    /// </summary>
    [HttpGet("by-node/{nodeId}")]
    public async Task<ActionResult<ApiResponse<List<VehicleDetectionData>>>> GetByNode(
        string nodeId,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        try
        {
            var detections = await _detectionService.GetDetectionsByNodeAsync(nodeId, from, to);
            return Ok(ApiResponse<List<VehicleDetectionData>>.SuccessResponse(detections));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت تشخیص‌های نود");
            return StatusCode(500, ApiResponse<List<VehicleDetectionData>>.ErrorResponse("خطا در دریافت تشخیص‌های نود"));
        }
    }

    /// <summary>
    /// دریافت آمار
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<ApiResponse<Dictionary<string, int>>>> GetStatistics()
    {
        try
        {
            var stats = await _detectionService.GetStatisticsAsync();
            return Ok(ApiResponse<Dictionary<string, int>>.SuccessResponse(stats));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت آمار");
            return StatusCode(500, ApiResponse<Dictionary<string, int>>.ErrorResponse("خطا در دریافت آمار"));
        }
    }
}

public class VehicleDetectionSubmitModel
{
    public VehicleDetectionData Detection { get; set; } = new();
    public IFormFile? Image { get; set; }
}


