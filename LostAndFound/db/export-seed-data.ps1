# =====================================================================
# Regenerate db/seed-data.sql from the CURRENT LostAndFound database.
# Run after you change the seed (or add demo rows) so teammates can import
# the exact data without running the C# seeder.
#
#   powershell -ExecutionPolicy Bypass -File LostAndFound/db/export-seed-data.ps1
#
# Then a teammate loads it on an EMPTY DB:
#   sqlcmd -S "(localdb)\MSSQLLocalDB" -b -i LostAndFound/db/schema.sql
#   sqlcmd -S "(localdb)\MSSQLLocalDB" -d LostAndFound -b -i LostAndFound/db/seed-data.sql
# =====================================================================
$ErrorActionPreference = "Stop"
[void][System.Reflection.Assembly]::LoadWithPartialName("Microsoft.SqlServer.SMO")
[void][System.Reflection.Assembly]::LoadWithPartialName("Microsoft.SqlServer.SmoExtended")
[void][System.Reflection.Assembly]::LoadWithPartialName("Microsoft.SqlServer.ConnectionInfo")

$srv = New-Object Microsoft.SqlServer.Management.Smo.Server("(localdb)\MSSQLLocalDB")
$srv.ConnectionContext.Connect()
$db = $srv.Databases["LostAndFound"]

$scr = New-Object Microsoft.SqlServer.Management.Smo.Scripter($srv)
$scr.Options.ScriptSchema = $false
$scr.Options.ScriptData = $true          # data only (INSERTs, with IDENTITY_INSERT)
$scr.Options.NoCommandTerminator = $false

# FK-safe order: roles/users, lookups, then the FoundItem graph, then audit.
$tables = "AspNetRoles","AspNetUsers","AspNetUserRoles","Category","Location","Tag","FoundItem","FoundItemImage","FoundItemTag","LostItem","LostItemImage","LostItemTag","AuditLog"
$objs = [Microsoft.SqlServer.Management.Smo.SqlSmoObject[]]($tables | ForEach-Object { $db.Tables[$_] })

$header = @"
/* =====================================================================
   LostAndFound - seed/demo DATA export (INSERTs only, no schema). GENERATED - do not hand-edit.
   Run on an EMPTY DB, right after schema.sql:
     sqlcmd -S "(localdb)\MSSQLLocalDB" -b -i LostAndFound/db/schema.sql
     sqlcmd -S "(localdb)\MSSQLLocalDB" -d LostAndFound -b -i LostAndFound/db/seed-data.sql
   Demo member login: userNN@lostandfound.local / Demo#12345
   ===================================================================== */
USE [LostAndFound];
GO
-- The Identity tables have filtered indexes, so inserts need QUOTED_IDENTIFIER ON (sqlcmd defaults it OFF).
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO
"@

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine($header)
# One statement per line; DO NOT put GO between lines (would break each table's IDENTITY_INSERT ON..OFF block).
foreach ($line in $scr.EnumScript($objs)) { [void]$sb.AppendLine($line) }
[void]$sb.AppendLine("GO")

$out = Join-Path $PSScriptRoot "seed-data.sql"
[System.IO.File]::WriteAllText($out, $sb.ToString(), (New-Object System.Text.UTF8Encoding($true)))
Write-Host "Wrote $out ($((Get-Item $out).Length) bytes)"
