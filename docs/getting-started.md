# Getting started

## Prerequisites

- .NET SDK 10.0.100+ (`global.json`)
- Git 2.40+
- For graphical samples: a desktop environment (Raylib apps)

## Option A — local monorepo (recommended for maintainers)

1. Clone or open `novolis-dogfooding` next to other `novolis-*` repos.
2. Run `./scripts/link-local-repos.ps1` to junction `submodules/` to sibling folders.
3. `dotnet build Novolis.Dogfooding.slnx`
4. `dotnet run --project apps/MathGridDemo`

Edits in `../novolis-math` are picked up on the next build without publishing packages.

## Option B — submodules from GitHub

```bash
git clone --recurse-submodules https://github.com/Novolis-Platform/novolis-dogfooding.git
cd novolis-dogfooding
dotnet build Novolis.Dogfooding.slnx
```

## Option C — refresh submodules

```powershell
./scripts/sync-submodules.ps1
# or
git submodule update --init --recursive
```
