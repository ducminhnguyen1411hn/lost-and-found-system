namespace LostAndFound.Models.ViewModels.Claims;

public class ClaimMessageViewModel
{
    public int Id { get; init; }
    public string SenderName { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    /// <summary>Sent by the viewer — drives left/right bubble alignment.</summary>
    public bool IsMine { get; init; }
}
