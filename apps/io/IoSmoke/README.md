# IoSmoke

Dogfoods **Novolis.IO.Paths**, **Recovery**, **Watching**, **Processes**, and **Git** from GitHub Packages (`2026.1.*`).

```powershell
dotnet restore
dotnet run --project apps/io/IoSmoke
```

Git status uses the dogfooding repo when a `.git` directory is present; otherwise the Git section is skipped with a message.
