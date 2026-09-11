# 33 Label

فروشگاه آنلاین **33 Label** — ASP.NET Core MVC، SQL Server، معماری Onion.  
ریپو: https://github.com/shahab-ahmadpour/Label33-

## اجرای لوکال (جایگزین Railway برای تست)

راهنمای کامل: **[docs/LOCAL_RUN.md](docs/LOCAL_RUN.md)**

### Windows (پیشنهادی — LocalDB + Migration)

```powershell
dotnet tool install -g dotnet-ef --version 8.0.11
.\scripts\run-local.ps1
```

باز کنید: [http://localhost:5072](http://localhost:5072)  
Ops Console: [http://localhost:5072/ops-33-console/login](http://localhost:5072/ops-33-console/login)  
`superadmin@33label.local` / `ChangeMe_33Label!`

### Docker SQL Server

```powershell
.\scripts\run-local.ps1 -Mode DockerSql
```

```bash
chmod +x scripts/run-local.sh
./scripts/run-local.sh docker
```

### Sqlite سریع (بدون SQL Server)

```powershell
.\scripts\run-local.ps1 -Mode Sqlite
```

تست‌ها را روی **localhost** انجام دهید، نه روی `https://label33-production.up.railway.app/`.

## ساختار

```
Label33.sln
src/Label33.Domain
src/Label33.Application
src/Label33.Infrastructure
src/Label33.Web
tests/Label33.Tests
docs/
scripts/
```

## دستورات پایه

```bash
dotnet restore
dotnet test
dotnet ef database update --project src/Label33.Infrastructure --startup-project src/Label33.Web
dotnet run --project src/Label33.Web --launch-profile Local
```

## محیط‌ها

| Environment | DB |
|-------------|-----|
| `Development` | Sqlite فایل `label33.dev.db` |
| `Local` | SQL Server LocalDB (`Label33Db`) + migrations |
| `DockerSql` | SQL Server Docker پورت 1433 + migrations |
| `Production` | env / Railway |

مفهوم برند: [docs/CONCEPT_LOCK.md](docs/CONCEPT_LOCK.md)
