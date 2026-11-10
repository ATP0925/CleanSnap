# Contributing to CleanSnap

We welcome contributions to CleanSnap! Whether you're adding support for new junk directories, optimizing xxHash algorithms, or fixing UI issues on Windows or Android, your help is appreciated.

## Getting Started
1. Fork the repo: `https://github.com/ATP0925/CleanSnap`
2. Clone your fork and open in Visual Studio 2022 or VS Code.
3. Install .NET 10 SDK and MAUI workload:
   ```bash
   dotnet workload install maui
   dotnet restore
   ```
4. Run locally:
   ```bash
   dotnet run -f net10.0-windows10.0.19041.0
   ```

## Contribution Rules
- Never remove safety checks around filesystem deletions.
- All file deletions must route through the OS Recycle Bin / Trash API.
- Maintain zero network telemetry: CleanSnap must always remain 100% offline.
