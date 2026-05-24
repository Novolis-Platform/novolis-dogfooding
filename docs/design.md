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

Version pin: `Directory.Packages.props` → `NovolisPackageVersion` (4-part, e.g. `0.0.1.1`).

## Local development

1. Set `NOVOLIS_GITHUB_PACKAGES_PAT` (or `GITHUB_TOKEN` / `GH_TOKEN`) with `read:packages`
2. `./scripts/build.ps1` or Rider Build Solution (runs `prepare-dogfood-packages.ps1` before restore)

## CI

Authenticate with `GITHUB_TOKEN`, `dotnet restore` from `nuget.config`, then build `Novolis.Dogfooding.slnx`.

## Related

- [nuget-setup.md](../../novolis-governance/docs/nuget-setup.md)
- [release-policy.md](../../novolis-governance/docs/release-policy.md)
