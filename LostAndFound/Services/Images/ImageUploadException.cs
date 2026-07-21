namespace LostAndFound.Services.Images;

public class ImageUploadException : Exception
{
    public ImageUploadException(string message) : base(message) { }
}
