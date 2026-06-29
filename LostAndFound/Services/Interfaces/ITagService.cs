using LostAndFound.Models.Entities;

namespace LostAndFound.Services.Interfaces;

/// <summary>
/// Shared contract (REQUIREMENTS §5). Dev A owns the implementation; both devs call it so tag
/// matching/subscribe always uses the SAME normalization. Do not change this signature without
/// agreeing with the other dev. No DI registration yet — wire it when the implementation lands.
/// </summary>
public interface ITagService
{
    /// <summary>The one and only tag normalizer: trim + lower + strip Vietnamese diacritics +
    /// collapse whitespace. Display uses the raw tag; matching ALWAYS uses this normalized form.</summary>
    string Normalize(string raw);

    /// <summary>Resolve raw tag strings to <see cref="Tag"/> rows, creating any that don't exist
    /// (deduped on NormalizedTag).</summary>
    Task<IEnumerable<Tag>> ResolveTagsAsync(IEnumerable<string> rawTags);
}
