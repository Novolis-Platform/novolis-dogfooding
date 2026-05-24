# Design

## Purpose

`novolis-dogfooding` validates the Novolis ecosystem **as package consumers**: library repos publish to **GitHub Packages**; this repo restores those packages via `PackageReference` only.

## Non-goals

- Publishing NuGet packages from this repository
- Replacing per-repo unit/integration tests
- `ProjectReference` into sibling `novolis-*` clones (use GitHub Packages instead)
- Local `artifacts/nuget-local` as the primary feed

## Package source

| Source | URL |
|--------|-----|
| GitHub Packages | `https://nuget.pkg.github.com/Novolis-Platform/index.json` |
| nuget.org | Third-party dependencies only (`packageSourceMapping` in `nuget.config`) |

Version pin: `Directory.Packages.props` → floating `2026.1.*` for Novolis packages.

## Local development

1. Authenticate to GitHub Packages (`gh auth refresh -s read:packages`, or set `NOVOLIS_GPR_TOKEN` / `GITHUB_TOKEN`)
2. `dotnet restore` then `dotnet build` (Rider: normal **Build Solution**)

## CI

Authenticate with `GITHUB_TOKEN`, `dotnet restore` from `nuget.config`, then build `Novolis.Dogfooding.slnx`.

## Related

- [nuget-setup.md](../../novolis-governance/docs/nuget-setup.md)
- [release-policy.md](../../novolis-governance/docs/release-policy.md)
