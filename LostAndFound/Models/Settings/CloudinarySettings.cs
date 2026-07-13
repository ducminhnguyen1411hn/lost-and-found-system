namespace LostAndFound.Models.Settings;

/// <summary>
/// Cloudinary credentials, bound from the <c>Cloudinary</c> configuration section
/// (real values live in user-secrets locally; never commit them). Used to build the
/// <see cref="global::CloudinaryDotNet.Cloudinary"/> client for found-item image uploads (FR-FOUND-05).
/// </summary>
public class CloudinarySettings
{
    public string CloudName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
}
