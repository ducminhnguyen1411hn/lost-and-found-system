# Dev Setup — Install · Build · Run in VSCode

Step-by-step guide to take a fresh machine from **clone → running** in VSCode.
Follow it in order. Stuck? jump to **[Troubleshooting](#8-troubleshooting)**.

> Technical context: the app targets **`net8.0`** (even though the machine's SDK may be .NET 10),
> the database is **SQL Server LocalDB**, and by convention this project is **DB-First — NO EF
> migrations**. So the database must be created by hand from `schema.sql` **before the first run**
> (see [step 4](#4-create-the-database-first-run--mandatory)).

---

## 0. Install prerequisites (one-time)

| What you need | How to check it's there | If missing |
|---|---|---|
| **.NET 8** (SDK, or at least the runtime) | `dotnet --list-runtimes` shows a `Microsoft.AspNetCore.App 8.0.x` line | Install the **.NET 8 SDK**: <https://dotnet.microsoft.com/download/dotnet/8.0> — or `winget install Microsoft.DotNet.SDK.8` |
| **SQL Server LocalDB** | `sqllocaldb info` prints `MSSQLLocalDB` | Install "SQL Server Express LocalDB" (ships with SQL Server Express, or via the Visual Studio Installer → *SQL Server Express 2019 LocalDB* component) |
| **sqlcmd** | `sqlcmd -?` runs | Install "SQL Server Command Line Utilities (sqlcmd)" — or use the alternative in [step 4](#4-create-the-database-first-run--mandatory) |
| **Git** | `git --version` | `winget install Git.Git` |
| **VSCode** + extensions | | Install **C# Dev Kit** (`ms-dotnettools.csdevkit`) — it pulls in **C#** + **.NET Install Tool**. Recommended extra: **SQL Server (mssql)** (`ms-mssql.mssql`) to browse/query the DB inside VSCode. |

> 💡 The project's original dev machine runs fine on **.NET 10 SDK + .NET 8 runtime (8.0.28)** — it
> builds and runs on 8. But a teammate's machine that has **only .NET 10** and is **missing the .NET 8
> runtime** will fail `dotnet run` with *"framework 8.0.0 was not found"*. Install the **.NET 8 SDK** to
> be safe.

---

## 1. Clone the repo (skip if you already have the code)

```bash
git clone <REPO-URL> LostAndFound
cd LostAndFound
```

## 2. Open the project in VSCode

Open the **repo root** (the folder containing `CLAUDE.md` and `docs/`), **not** the inner
`LostAndFound/` subfolder:

```bash
code .
```

On first open, **C# Dev Kit** picks up `LostAndFound/LostAndFound.csproj` automatically (the repo has
no `.sln`; Dev Kit works directly off the `.csproj`). Wait for *"Loading projects…"* in the bottom-right
to finish.

## 3. Restore packages

```bash
dotnet restore LostAndFound/LostAndFound.csproj
```

(C# Dev Kit usually restores on open — run this manually if you want to be sure.)

## 4. Create the database (first run) — **MANDATORY**

This project is **DB-First with no migrations**, so it **does not create tables itself**. On startup,
[`Program.cs`](../LostAndFound/Program.cs) only *seeds* the 4 roles + the admin account into tables that
must already exist. **If the DB isn't created, the app crashes during seeding.** Run this once (and again
whenever `schema.sql` changes):

```bash
sqlcmd -S "(localdb)\MSSQLLocalDB" -b -i LostAndFound/db/schema.sql
```

In VSCode you can instead use **Terminal → Run Task… → `db:recreate`** (preconfigured in `.vscode/tasks.json`).

> No `sqlcmd`? Use the **SQL Server (mssql)** extension: connect to `(localdb)\MSSQLLocalDB`, open
> `LostAndFound/db/schema.sql`, and hit **Execute Query** (Ctrl+Shift+E).

## 5. (Optional) User-secrets for Cloudinary

Only needed for the **image/video upload** feature (`IMediaService`). **Skippable** for the first run.
Keys must **not** live in `appsettings.json`:

```bash
dotnet user-secrets set "Cloudinary:CloudName" "xxx" --project LostAndFound/LostAndFound.csproj
dotnet user-secrets set "Cloudinary:ApiKey"    "xxx" --project LostAndFound/LostAndFound.csproj
dotnet user-secrets set "Cloudinary:ApiSecret" "xxx" --project LostAndFound/LostAndFound.csproj
```

## 6. Build

```bash
dotnet build LostAndFound/LostAndFound.csproj
```

Expected: **0 Errors** (there are currently a few `CS8618`/`CS860x` nullable *warnings* from the auth
feature — **harmless**, not something you broke). In VSCode: **Ctrl+Shift+B** → pick the `build` task.

## 7. Run

**Option A — press F5 (recommended, with debugging):**
Open **Run and Debug** (Ctrl+Shift+D) → select **".NET Core Launch (web, http)"** → press **F5**. It
builds, runs, attaches the debugger, and opens the browser at <http://localhost:5082>. Set breakpoints
in code and they hit.

**Option B — terminal (no debugging):**
```bash
dotnet run --project LostAndFound/LostAndFound.csproj
```
Then open <http://localhost:5082>. Stop with **Ctrl+C**.

> Want HTTPS too: `dotnet run --project LostAndFound/LostAndFound.csproj --launch-profile https`
> (port `https://localhost:7257`). Run `dotnet dev-certs https --trust` once to trust the dev cert.

### Test login
- **Seeded admin:** `admin@lostandfound.local` / `Admin#12345`
- Login page: `/Identity/Account/Login` · Register a new member: `/Register`

> ⚠️ Change the admin password before any real deployment.

---

## 8. Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| `The framework 'Microsoft.NETCore.App', version '8.0.0' was not found` | .NET 8 runtime is missing | Install the **.NET 8 SDK** (section 0), then re-check `dotnet --list-runtimes` |
| `Cannot open database "LostAndFound"` / error during startup seeding | The DB wasn't created from `schema.sql` | Re-run [step 4](#4-create-the-database-first-run--mandatory) |
| `sqllocaldb` reports the instance isn't running | LocalDB is stopped | `sqllocaldb start MSSQLLocalDB` |
| `'sqlcmd' is not recognized` | sqlcmd isn't installed | Install the SQL cmd utilities, **or** use the mssql extension (step 4) |
| Browser doesn't open / port in use | Port 5082 is taken | Change the port in `LostAndFound/Properties/launchSettings.json` (`http` profile) and in `.vscode/launch.json` (`ASPNETCORE_URLS`) |
| Warning `Failed to determine the https port for redirect` | Running http-only, so there's no https port to redirect to | Harmless in dev — ignore, or run the `https` profile as above |
| F5 shows no config / prompts for an extension | C# Dev Kit isn't installed | Install `ms-dotnettools.csdevkit`, reload VSCode |
| Edited `schema.sql` but the C# entities didn't change | Entities are **generated**, not hand-edited | Run the **`/db-rescaffold`** skill (see [INDEX §11](INDEX.md)) — don't hand-edit `Models/Entities/` |

---

## Cheat sheet

```bash
# create/recreate the DB (whenever schema.sql changes)
sqlcmd -S "(localdb)\MSSQLLocalDB" -b -i LostAndFound/db/schema.sql

dotnet build LostAndFound/LostAndFound.csproj              # build
dotnet run   --project LostAndFound/LostAndFound.csproj    # run → http://localhost:5082
```

See also: [`INDEX.md`](INDEX.md) (overview) · [`BASE_SETUP.md`](BASE_SETUP.md) (how the foundation was
built) · [`../CLAUDE.md`](../CLAUDE.md) (code conventions).
