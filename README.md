# 33 Label

فروشگاه آنلاین **33 Label** — پروژه مستقل با Backend مبتنی بر ASP.NET Core MVC، SQL Server و معماری Onion.

## وضعیت

فاز فعلی: **اسکلت Backend + Domain + Application + Infrastructure + Migration اولیه** (UI نهایی هنوز نه).

- [docs/ANALYSIS.md](docs/ANALYSIS.md)

## ساختار

```
Label33.sln
src/Label33.Domain
src/Label33.Application
src/Label33.Infrastructure
src/Label33.Web
tests/Label33.Tests
```

## اجرا (توسعه)

```bash
cd Label33
dotnet restore
dotnet test
dotnet ef database update --project src/Label33.Infrastructure --startup-project src/Label33.Web
dotnet run --project src/Label33.Web
```

- Production/SQL Server: `Database:Provider=SqlServer` + connection string `Label33Db`
- Development پیش‌فرض: Sqlite فایل `label33.dev.db` برای smoke بدون SQL Server

## ترتیب کار

1. زیرساخت + دیتابیس + Migrations + منطق پشت‌صحنه (در حال تکمیل)
2. UI/UX طبق داکیومنت جداگانه (هنوز ارسال نشده)
