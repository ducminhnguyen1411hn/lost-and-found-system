using LostAndFound.Models.ViewModels.Common;
using LostAndFound.Models.ViewModels.Items;

namespace LostAndFound.Services.Interfaces;

public interface IItemBoardService
{
    /// <summary>Unified public board: found + lost, filtered, sorted newest-first, paginated.</summary>
    Task<PagedResult<BoardItemViewModel>> SearchAsync(BoardSearchViewModel q);
}
