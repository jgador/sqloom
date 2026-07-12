# Sqloom Command Documentation

Use the Sqloom command reference at [.agents/skills/sqloom/references/commands.md](../.agents/skills/sqloom/references/commands.md), or run `sqloom help <command>` from an installed tool, for exact command syntax, required options, option names, allowed values, defaults, and command-specific notes.

Keep this page focused on operational context that does not belong in the command table.

## SQL Server Permissions

When running Sqloom against SQL Server or Azure SQL, use a dedicated read-only database principal with enough access to read Query Store, metadata, and showplans. Use the command reference for the current commands that consume the connection string.

```sql
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'sqloom_ro')
BEGIN
    CREATE USER [sqloom_ro] WITH PASSWORD = N'<strong-password>';
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.database_role_members AS drm
    INNER JOIN sys.database_principals AS roles
        ON roles.principal_id = drm.role_principal_id
    INNER JOIN sys.database_principals AS members
        ON members.principal_id = drm.member_principal_id
    WHERE roles.name = N'db_datareader'
        AND members.name = N'sqloom_ro')
BEGIN
    ALTER ROLE [db_datareader] ADD MEMBER [sqloom_ro];
END;

GRANT VIEW DATABASE PERFORMANCE STATE TO [sqloom_ro];
GRANT VIEW DEFINITION TO [sqloom_ro];
GRANT SHOWPLAN TO [sqloom_ro];
DENY INSERT TO [sqloom_ro];
DENY UPDATE TO [sqloom_ro];
DENY DELETE TO [sqloom_ro];
```

If the host reports that module discovery was skipped because `VIEW DEFINITION` is unavailable, grant that permission when you want stored procedures and functions included in app classification.
