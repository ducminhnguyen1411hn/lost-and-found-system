namespace LostAndFound.Services.Images;

/// <summary>
/// The <see cref="IImageUploadService"/> the app actually uses. Tries Cloudinary first (the real host);
/// if Cloudinary is UNREACHABLE (SSL / timeout / connection refused — e.g. a firewall blocking
/// <c>api.cloudinary.com</c>), it saves the image locally instead, so an upload never hard-fails.
///
/// A genuine <see cref="ImageUploadException"/> (wrong type / too big) is NOT masked — it propagates,
/// because the fallback would fail on it too. Only network-level failures trigger the local fallback.
/// </summary>
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

    /// <inheritdoc />
    public async Task<string?> UploadAsync(IFormFile? file, string folder)
    {
        try
        {
            return await _cloud.UploadAsync(file, folder);
        }
        catch (ImageUploadException)
        {
            throw; // real validation problem with the file — don't hide it behind the fallback
        }
        catch (Exception ex)
        {
            // Network-level failure: Cloudinary is unreachable (blocked / offline). Save locally so the
            // claim/post still goes through — the image just lives on this server instead of the CDN.
            _logger.LogWarning(ex, "Cloudinary upload failed ({Message}); falling back to local storage.", ex.Message);
            return await _local.UploadAsync(file, folder);
        }
    }
}
