using LostAndFound.Models.ViewModels.Common;
using LostAndFound.Models.ViewModels.Items;

namespace LostAndFound.Services.Interfaces;

public interface IItemBoardService
{
    Task<PagedResult<BoardItemViewModel>> SearchAsync(BoardSearchViewModel q, string? ownerUserId = null);
}
