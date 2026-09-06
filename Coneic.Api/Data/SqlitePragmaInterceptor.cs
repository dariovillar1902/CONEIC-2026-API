using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Coneic.Api.Data;

/// <summary>
/// SQLite is a single-writer database: without this, concurrent writers
/// (e.g. hundreds of people confirming an activity choice at the same time)
/// get an immediate "database is locked" error instead of queueing. A busy
/// timeout makes a blocked writer wait (and retry internally) instead of
/// failing outright. Applied on every physical connection EF Core opens.
///
/// NOTE: deliberately NOT setting journal_mode=WAL — the DB file lives on
/// Azure App Service's persistent /home mount, which is network storage
/// (Azure Files/SMB). WAL requires proper shared-memory/mmap support that
/// network filesystems don't reliably provide; enabling it here crashed the
/// app on startup with "SQLite Error 8: attempt to write a readonly
/// database" (confirmed in prod, 2026-09-01). Stick with the default
/// rollback-journal mode.
/// </summary>
public class SqlitePragmaInterceptor : DbConnectionInterceptor
{
    private const string Pragmas = "PRAGMA busy_timeout=30000;";

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = Pragmas;
        cmd.ExecuteNonQuery();
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = Pragmas;
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}
