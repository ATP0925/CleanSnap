# 🧹 CleanSnap

> **Cross-Platform 1-Tap Device Cleaner & Duplicate Finder for Windows & Android**  
> *Built with .NET MAUI • 100% Offline • Safe Recycle Bin Deletions • Store Ready*

---

## 🌟 Overview

**CleanSnap** is an open-source, privacy-focused device cleaning app that eliminates redundant duplicate files and bloatware caches across your devices with a single tap. 

Unlike traditional cleaners that harvest user data, CleanSnap operates **completely offline** with **zero network telemetry** and integrates with your OS Recycle Bin / Trash so you never risk accidental file loss.

---

## 🚀 Key Features

- **⚡ 4-Stage Multi-Pass Detection Pipeline**:
  1. *Async Directory Enumeration*: Non-blocking streaming traversal of user folders.
  2. *Size Bucketing ($O(1)$ Hash Map)*: Immediately eliminates 70–90% of files without reading disk content.
  3. *Partial xxHash64 Edge Hashing*: Reads first 4KB + last 4KB at ~30 GB/s for instant heuristic pruning.
  4. *Full Cryptographic SHA-256 Verification*: Streams 64KB chunks to guarantee 100% byte-for-byte fidelity.
- **🛡️ 100% Safe Deletions**:
  - **Windows**: Moves files directly to the native **Windows Recycle Bin** (`Microsoft.VisualBasic.FileIO`).
  - **Android**: Supports Android 11+ Scoped Storage and `MANAGE_EXTERNAL_STORAGE` guidelines.
- **💾 Local SQLite Database Audit Trail**:
  - Caches cryptographic hashes for $10\times$ to $50\times$ faster re-scans.
  - Maintains full audit log of all recycled files and cleanup sessions.
- **🗑️ Junk & Cache Cleaning**:
  - Cleans Windows `%TEMP%`, Python pip cache, application `.cache`, and crash dumps.
  - Gracefully skips active or locked files without crashing.
- **📱 Store-Ready Packaging**:
  - Ready for **Microsoft Store (MSIX)** with `broadFileSystemAccess` capability declarations.
  - Ready for **Google Play Store (AAB/APK)** with permission declaration forms.

---

## 🏗️ Architecture

```
CleanSnap/
├── CleanSnap.csproj              # Cross-compiles for Android & Windows
├── MauiProgram.cs                # Dependency injection & service lifecycle
├── Database/
│   ├── CleanupHistoryRecord.cs   # SQLite session history
│   ├── RecycledFileBackupLog.cs  # Audit trail of recycled files
│   └── FileHashCacheRecord.cs    # SHA-256 cache
├── Models/
│   ├── FileMetadata.cs           # File path, size, date, hashes
│   ├── DuplicateGroup.cs         # Master original + duplicate copies
│   ├── ScanResult.cs             # Scan statistics & groups
│   └── JunkCategory.cs           # Temp folders, sizes, descriptions
├── Services/
│   ├── Interfaces/
│   │   ├── IFileSystemService.cs
│   │   ├── IDuplicateScannerService.cs
│   │   ├── IJunkCleanerService.cs
│   │   └── IDatabaseService.cs
│   ├── DuplicateScannerService.cs # 4-stage duplicate detection engine
│   ├── JunkCleanerService.cs      # Cross-platform cache cleaner
│   └── DatabaseService.cs         # Local SQLite operations
├── Platforms/
│   ├── Windows/Services/WindowsFileService.cs
│   └── Android/Services/AndroidFileService.cs
├── ViewModels/
│   ├── HomeViewModel.cs          # Dashboard & 1-tap cleaner
│   ├── ScanViewModel.cs          # Multi-stage live progress
│   ├── ResultsViewModel.cs       # Duplicate inspection & selective cleanup
│   └── SettingsViewModel.cs      # Safety toggles & audit history
└── Views/
    ├── HomePage.xaml
    ├── ScanPage.xaml
    ├── ResultsPage.xaml
    └── SettingsPage.xaml
```

---

## 🛠️ Build & Run Instructions

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/)
- .NET MAUI Workloads (`dotnet workload install maui`)
- Windows 10/11 or Android SDK 34+

### Clone & Restore
```bash
git clone https://github.com/ATP0925/CleanSnap.git
cd CleanSnap
dotnet restore
```

### Run Locally on Windows
```bash
dotnet run -f net10.0-windows10.0.19041.0
```

### Build Release for Microsoft Store (MSIX)
```bash
dotnet publish -f net10.0-windows10.0.19041.0 -c Release
```

### Build Release for Google Play Store (Android AAB)
```bash
dotnet publish -f net10.0-android -c Release
```

---

## 📄 License
This project is open-source under the [MIT License](LICENSE).
