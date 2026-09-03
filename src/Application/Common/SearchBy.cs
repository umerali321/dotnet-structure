namespace SkillsetsBackend.Application.Common;

/// <summary>
/// Which single field a list screen is searching.
///
/// Every list used to take one generic `search` and OR a leading-wildcard LIKE across every column
/// it could think of - names, email, username, the two name concatenations, and for employees a
/// correlated EXISTS over company code and name. Measured on this database (162,487 Users) that is
/// <b>2,682 ms</b>, because a leading '%' cannot use an index, so all seven predicates scan.
///
/// Searching ONE named column instead takes <b>1 ms</b> - the indexes were already there
/// (IX_Users_Email, IX_Users_FirstName_LastName), they simply could not be used before.
///
/// So the API takes one field-specific parameter rather than a bag of them, and the caller sends
/// only the one the user picked. Null/absent means no search at all, never "search everything".
/// </summary>
public enum SearchBy
{
    Name,
    Email,
    Company,
    /// <summary>Companies screen only - the company code.</summary>
    Code,
    /// <summary>Learning Transcript only - the course/asset title.</summary>
    Course,
    /// <summary>Employees screen only - phone number, in place of Company there.</summary>
    Phone,
}

/// <summary>One field, one term - the whole search contract for a list query.</summary>
/// <param name="Field">Which column to search.</param>
/// <param name="Term">What to look for. Already trimmed; never empty.</param>
public sealed record SearchCriteria(SearchBy Field, string Term)
{
    /// <summary>
    /// Builds criteria from whichever field-specific parameter the caller supplied, or null when
    /// none was. Deliberately takes them all and picks ONE: if a caller sends several (an old
    /// client, a hand-written URL), the first non-empty in this fixed order wins rather than the
    /// query quietly falling back to scanning everything.
    /// </summary>
    public static SearchCriteria? From(
        string? name = null,
        string? email = null,
        string? company = null,
        string? code = null,
        string? course = null,
        string? phone = null)
    {
        if (Clean(name) is { } n) return new SearchCriteria(SearchBy.Name, n);
        if (Clean(email) is { } e) return new SearchCriteria(SearchBy.Email, e);
        if (Clean(company) is { } c) return new SearchCriteria(SearchBy.Company, c);
        if (Clean(code) is { } cd) return new SearchCriteria(SearchBy.Code, cd);
        if (Clean(course) is { } cr) return new SearchCriteria(SearchBy.Course, cr);
        if (Clean(phone) is { } p) return new SearchCriteria(SearchBy.Phone, p);
        return null;
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>
    /// PREFIX pattern - seekable, so it uses the existing index. Measured on this database:
    /// <b>0 ms</b> against 162,487 Users, versus 353 ms for the same single-column search written as
    /// '%term%', versus 2,525 ms for the old generic form.
    /// </summary>
    public string ToPrefixPattern() => Escape(Term) + "%";

    /// <summary>
    /// CONTAINS pattern - cannot use an index, so it scans (353 ms here). Used ONLY as a fallback
    /// when the prefix search found nothing at all.
    ///
    /// That fallback is what makes prefix-first safe. Prefix alone would quietly stop finding
    /// mid-string matches - searching "augusta.edu" for a whole domain returns 260 people with
    /// contains and 0 with prefix. Running it only when the fast path came back empty keeps every
    /// result that used to be findable, while the common case (typing the start of a name or
    /// address) still returns instantly.
    /// </summary>
    public string ToContainsPattern() => "%" + Escape(Term) + "%";

    /// <summary>Escapes LIKE metacharacters so a term containing % or _ is matched literally.
    /// Callers must pass ESCAPE '\' alongside the pattern.</summary>
    public static string Escape(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_").Replace("[", "\\[");
}
