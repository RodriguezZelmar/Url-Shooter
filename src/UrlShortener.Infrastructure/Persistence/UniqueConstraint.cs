using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace UrlShortener.Infrastructure.Persistence;

/// <summary>
/// Recognises unique-constraint violations. Each provider reports them with its own exception type,
/// so this is the only provider-specific piece of the repositories.
/// </summary>
internal static class UniqueConstraint
{
    // https://www.sqlite.org/rescode.html#constraint_unique
    private const int SqliteConstraintUnique = 2067;

    public static bool IsViolation(DbUpdateException exception) => exception.InnerException switch
    {
        SqliteException sqlite => sqlite.SqliteExtendedErrorCode == SqliteConstraintUnique,
        _ => false,
    };
}
