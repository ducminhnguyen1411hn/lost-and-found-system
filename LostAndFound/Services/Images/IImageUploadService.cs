namespace LostAndFound.Services.Images;

public interface IImageUploadService
{
    Task<string?> UploadAsync(IFormFile? file, string folder);
}
