using LostAndFound.Models.Entities;

namespace LostAndFound.Services.Interfaces;

public interface ITagService
{
    string Normalize(string raw);

    Task<IEnumerable<Tag>> ResolveTagsAsync(IEnumerable<string> rawTags);

    Task<IEnumerable<string>> SuggestTagsAsync(string partial);
}
