namespace LostAndFound.Services.Images;

/// <summary>Uploads a user-supplied image and returns the stored URL (FR-FOUND-05).</summary>
public interface IImageUploadService
{
    /// <summary>
    /// Uploads <paramref name="file"/> to the given <paramref name="folder"/>. Returns the hosted
    /// (secure) URL, or <c>null</c> when no file was supplied. Throws
    /// <see cref="ImageUploadException"/> on an invalid content-type or oversize file.
    /// </summary>
    Task<string?> UploadAsync(IFormFile? file, string folder);
}
