using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using LostAndFound.Data;
using LostAndFound.Models.Entities;
using LostAndFound.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Services;

/// <summary>
/// The single source of tag normalization (FR-TAG-01). Display uses the raw tag; ALL matching /
/// subscription compares on <see cref="Normalize"/>d form. See <see cref="ITagService"/>.
/// </summary>
public class TagService : ITagService
{
    private static readonly Regex MultiSpace = new(@"\s+", RegexOptions.Compiled);
    private readonly ApplicationDbContext _db;

    public TagService(ApplicationDbContext db) => _db = db;

    /// <inheritdoc />
    public string Normalize(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

        // 1) trim + lower, 2) map đ/Đ (they do NOT decompose under NFD),
        var lowered = raw.Trim().ToLowerInvariant().Replace('đ', 'd').Replace('Đ', 'd');

        // 3) strip VN diacritics via NFD then drop the combining marks,
        var decomposed = lowered.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }
        var stripped = sb.ToString().Normalize(NormalizationForm.FormC);

        // 4) collapse internal whitespace.
        return MultiSpace.Replace(stripped, " ").Trim();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Tag>> ResolveTagsAsync(IEnumerable<string> rawTags)
    {
        // normalized key -> first display form seen
        var map = new Dictionary<string, string>();
        foreach (var raw in rawTags ?? Enumerable.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            var norm = Normalize(raw);
            if (norm.Length == 0) continue;
            if (!map.ContainsKey(norm)) map[norm] = raw.Trim();
        }
        if (map.Count == 0) return Array.Empty<Tag>();

        var keys = map.Keys.ToList();
        var existing = await _db.Tag.Where(t => keys.Contains(t.NormalizedTag)).ToListAsync();
        var existingKeys = existing.Select(t => t.NormalizedTag).ToHashSet();

        var result = new List<Tag>(existing);
        foreach (var (norm, display) in map)
        {
            if (existingKeys.Contains(norm)) continue;
            // Added but NOT saved here — the caller's transaction owns the SaveChanges (FR-LOG-02).
            var tag = new Tag { DisplayTag = display, NormalizedTag = norm };
            _db.Tag.Add(tag);
            result.Add(tag);
        }
        return result;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> SuggestTagsAsync(string partial)
    {
        if (string.IsNullOrWhiteSpace(partial)) return Array.Empty<string>();

        var norm = Normalize(partial);
        if (norm.Length == 0) return Array.Empty<string>();

        // Search tags where NormalizedTag contains the normalized partial, return DisplayTag for UI
        return await _db.Tag.AsNoTracking()
            .Where(t => t.NormalizedTag.Contains(norm))
            .OrderBy(t => t.DisplayTag)
            .Select(t => t.DisplayTag)
            .Take(10)
            .ToListAsync();
    }
}
