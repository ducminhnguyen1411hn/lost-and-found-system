namespace LostAndFound.Services.Images;

/// <summary>Thrown for a user-correctable upload problem (bad type/size or provider error).
/// Controllers translate it into a ModelState error rather than a 500.</summary>
public class ImageUploadException : Exception
{
    public ImageUploadException(string message) : base(message) { }
}
