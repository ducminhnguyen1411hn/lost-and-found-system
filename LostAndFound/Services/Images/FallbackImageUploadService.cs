namespace LostAndFound.Services.Images;

public class FallbackImageUploadService : IImageUploadService
{
    private readonly CloudinaryImageUploadService _cloud;
    private readonly LocalImageUploadService _local;
    private readonly ILogger<FallbackImageUploadService> _logger;

    public FallbackImageUploadService(
        CloudinaryImageUploadService cloud,
        LocalImageUploadService local,
        ILogger<FallbackImageUploadService> logger)
    {
        _cloud = cloud;
        _local = local;
        _logger = logger;
    }

    public async Task<string?> UploadAsync(IFormFile? file, string folder)
    {
        try
        {
            return await _cloud.UploadAsync(file, folder);
        }
        catch (ImageUploadException)
        {
            throw;
        }
        catch (Exception ex)
        {

            _logger.LogWarning(ex, "Cloudinary upload failed ({Message}); falling back to local storage.", ex.Message);
            return await _local.UploadAsync(file, folder);
        }
    }
}
