namespace LostAndFound.Models.Enums;

/// <summary>
/// Lifecycle of a <c>CameraCheckRequest</c>. Stored as <c>int</c>
/// (column CameraCheckRequest.Status, CHECK 0..3). LOCKED CONTRACT.
/// </summary>
public enum CameraRequestStatus
{
    Pending = 0,
    InReview = 1,
    Resolved = 2,
    Rejected = 3
}
