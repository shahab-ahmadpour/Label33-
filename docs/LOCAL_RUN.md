# اجرای لوکال 33 Label (به‌جای Railway)

به‌جای تست روی `https://label33-production.up.railway.app/` از ماشین خودتان استفاده کنید.

آدرس لوکال پیش‌فرض: **http://localhost:5072**

---

## پیش‌نیاز

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- یکی از این‌ها برای SQL Server واقعی + Migration:
  - **Windows:** SQL Server LocalDB (همراه Visual Studio)
  - **همه OSها:** Docker Desktop
- یا فقط برای smoke سریع: **Sqlite** (بدون SQL Server)

```bash
dotnet tool install -g dotnet-ef --version 8.0.11
# یا آپدیت:
dotnet tool update -g dotnet-ef --version 8.0.11
```

کلون / pull:

```bash
git clone https://github.com/shahab-ahmadpour/Label33-.git
cd Label33-
git checkout cursor/ops-33-console-0a1a   # یا main بعد از merge
```

---

## راه سریع (پیشنهادی)

### Windows + LocalDB

```powershell
cd Label33-
.\scripts\run-local.ps1
# یا صریح:
.\scripts\run-local.ps1 -Mode LocalDb
```

### Windows / macOS / Linux + Docker SQL

```powershell
.\scripts\run-local.ps1 -Mode DockerSql
```

```bash
chmod +x scripts/run-local.sh
./scripts/run-local.sh docker
```

### فقط Sqlite (بدون Docker/LocalDB)

```powershell
.\scripts\run-local.ps1 -Mode Sqlite
```

```bash
./scripts/run-local.sh sqlite
```

---

## دستی (اگر اسکریپت نخواستید)

### 1) LocalDB (Windows)

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Local"
sqllocaldb start MSSQLLocalDB
dotnet restore
dotnet ef database update --project src/Label33.Infrastructure --startup-project src/Label33.Web
dotnet run --project src/Label33.Web --launch-profile Local --urls http://localhost:5072
```

### 2) Docker SQL Server

```bash
docker compose up -d sql
export ASPNETCORE_ENVIRONMENT=DockerSql   # PowerShell: $env:ASPNETCORE_ENVIRONMENT="DockerSql"
dotnet ef database update --project src/Label33.Infrastructure --startup-project src/Label33.Web
dotnet run --project src/Label33.Web --launch-profile DockerSql --urls http://localhost:5072
```

رمز SA در `docker-compose.yml`: `Label33_Strong_Pass!`

### 3) Sqlite توسعه

```bash
export ASPNETCORE_ENVIRONMENT=Development
dotnet run --project src/Label33.Web --launch-profile http --urls http://localhost:5072
```

Sqlite از `EnsureCreated` استفاده می‌کند (مایگریشن SQL Server را روی فایل Sqlite اعمال نمی‌کند).

---

## آدرس‌های تست لوکال

| چه چیزی | URL |
|--------|-----|
| فروشگاه | http://localhost:5072/ |
| Ops Console | http://localhost:5072/ops-33-console/login |
| SuperAdmin | `superadmin@33label.local` / `ChangeMe_33Label!` |

Railway فقط برای production بماند؛ تست UI/API را روی localhost انجام دهید.

---

## پروفایل‌های محیط

| Environment | Provider | فایل تنظیمات |
|-------------|----------|--------------|
| `Development` | Sqlite | `appsettings.Development.json` |
| `Local` | SQL Server LocalDB | `appsettings.Local.json` |
| `DockerSql` | SQL Server در Docker | `appsettings.DockerSql.json` |
| `Production` | Sqlite روی Railway (فعلی) یا SQL با env | `appsettings.Production.json` |

روی استارتاپ، برای SqlServer اپلیکیشن `MigrateAsync` را هم صدا می‌زند؛ با این حال اجرای صریح `dotnet ef database update` قبل از run توصیه‌شده است.

## Sms OTP (storefront login)

Default provider is `Development` (writes SMS files under `App_Data/sms` and can show the code on the login page).

When ready for Kavenegar, set in `appsettings`:

```json
"Sms": {
  "Provider": "Kavenegar",
  "Kavenegar": { "ApiKey": "YOUR_KEY", "Sender": "YOUR_SENDER" }
}
```

Ops console login remains email/password.
