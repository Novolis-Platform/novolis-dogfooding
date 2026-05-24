# Getting started

## Prerequisites

- .NET SDK 10.0.100+ (`global.json`)
- Git 2.40+
- GitHub CLI (`gh`) with `read:packages`, or a PAT in `NOVOLIS_GPR_TOKEN` / `GITHUB_TOKEN`
- For graphical samples: a desktop environment (Raylib apps)

## Clone and build

```powershell
git clone https://github.com/Novolis-Platform/novolis-dogfooding.git
cd novolis-dogfooding
gh auth refresh -h github.com -s read:packages
dotnet restore
dotnet build --no-restore
dotnet run --project apps/MathGridDemo
```
