# Design

## Purpose

`novolis-dogfooding` validates the Novolis ecosystem **as consumers build it**: library repos stay independent and publishable; this repo composes them via submodules and project references.

## Non-goals

- Publishing NuGet packages from this repository
- Replacing per-repo unit/integration tests
- Vendoring copies of library source (submodules stay authoritative)

## Submodule set

`.novolis/repos.json` lists library and domain repositories. Platform infra (`novolis-workflows`, `novolis-governance`, `novolis-registry`, `novolis-install`, `novolis-template-dotnet`, `novolis-smoketest`) is excluded because they are not application dependencies.

## Apps

Each app under `apps/` is a minimal executable:

- **MathGridDemo** — `Novolis.Math.Arrays` smoke
- **RaylibHello** — `Novolis.Raylib` window smoke

New apps should stay small and focused on one integration surface.

## MSBuild

- Root `Directory.Build.props` sets `IsPackable=false` and `NovolisSubmoduleRoot`.
- Apps use `$(NovolisSubmoduleRoot)novolis-<domain>/…` project references.

## CI

GitHub Actions checks out with `submodules: recursive` and runs `dotnet build` on the solution. No `dotnet test` at this layer unless dedicated dogfood tests are added later.
