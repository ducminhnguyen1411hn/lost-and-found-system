namespace LostAndFound.Services.Images;

public class LocalImageUploadService : IImageUploadService
{
    private static readonly string[] AllowedTypes = { "image/jpeg", "image/png", "image/webp" };
    private const long MaxBytes = 5 * 1024 * 1024;

    private readonly IWebHostEnvironment _env;

    public LocalImageUploadService(IWebHostEnvironment env) => _env = env;

    public async Task<string?> UploadAsync(IFormFile? file, string folder)
    {
        if (file is null || file.Length == 0) return null;

        if (!AllowedTypes.Contains(file.ContentType))
            throw new ImageUploadException("Ảnh phải có định dạng JPG, PNG hoặc WEBP.");
        if (file.Length > MaxBytes)
            throw new ImageUploadException("Ảnh không được vượt quá 5MB.");

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext))
            ext = file.ContentType switch { "image/png" => ".png", "image/webp" => ".webp", _ => ".jpg" };

        var safeFolder = string.IsNullOrWhiteSpace(folder) ? "misc" : folder.Trim('/', '\\');
        var root = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var dir = Path.Combine(root, "uploads", safeFolder);
        Directory.CreateDirectory(dir);

        var name = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(dir, name);
        await using (var dest = new FileStream(fullPath, FileMode.Create))
        await using (var src = file.OpenReadStream())
            await src.CopyToAsync(dest);

        return $"/uploads/{safeFolder}/{name}";
    }
}
