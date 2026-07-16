using LostAndFound.Models.ViewModels.Common;
using LostAndFound.Models.ViewModels.Items;

namespace LostAndFound.Services.Interfaces;

public interface IItemBoardService
{
    /// <summary>
    /// Unified board: found + lost, filtered, sorted newest-first, paginated.
    /// <para><paramref name="ownerUserId"/> null = the PUBLIC board (only Open items, every owner).
    /// Non-null = that user's own posts in EVERY status.</para>
    /// <para>It is a separate parameter on purpose — it must never be model-bound. If it lived on
    /// <see cref="BoardSearchViewModel"/>, anyone could pass <c>?OwnerUserId=&lt;someone-else&gt;</c> and
    /// read another user's non-Open (hidden) posts. The controller passes the signed-in user's id.</para>
    /// </summary>
    Task<PagedResult<BoardItemViewModel>> SearchAsync(BoardSearchViewModel q, string? ownerUserId = null);
}
