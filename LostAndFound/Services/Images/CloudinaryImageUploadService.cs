using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace LostAndFound.Services.Images;

public class CloudinaryImageUploadService : IImageUploadService
{
    private static readonly string[] AllowedTypes = { "image/jpeg", "image/png", "image/webp" };
    private const long MaxBytes = 5 * 1024 * 1024;

    private readonly Cloudinary _cloudinary;

    public CloudinaryImageUploadService(Cloudinary cloudinary) => _cloudinary = cloudinary;

    public async Task<string?> UploadAsync(IFormFile? file, string folder)
    {
        if (file is null || file.Length == 0) return null;

        if (!AllowedTypes.Contains(file.ContentType))
            throw new ImageUploadException("Ảnh phải có định dạng JPG, PNG hoặc WEBP.");
        if (file.Length > MaxBytes)
            throw new ImageUploadException("Ảnh không được vượt quá 5MB.");

        await using var stream = file.OpenReadStream();
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = folder
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var result = await _cloudinary.UploadAsync(uploadParams, cts.Token);
        if (result.Error is not null)
            throw new ImageUploadException("Tải ảnh lên thất bại: " + result.Error.Message);

        return result.SecureUrl?.ToString();
    }
}
