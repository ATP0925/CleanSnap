# Privacy Policy for CleanSnap

Last updated: October 30, 2026

CleanSnap ("we", "our", or "the app") is designed from the ground up as a **privacy-first, 100% offline device cleaning utility**.

### 1. No Data Collection or Telemetry
CleanSnap does not collect, transmit, store, or share any personal information, files, metadata, device identifiers, or analytics.

### 2. File Access Permissions
- **Android (`MANAGE_EXTERNAL_STORAGE`)**: CleanSnap requests file system access solely on-device to inspect user directories (Downloads, Documents, DCIM, Pictures) for byte-for-byte duplicate copies and temporary cache files. No file contents or paths are ever transmitted over the network.
- **Windows (`broadFileSystemAccess`)**: CleanSnap uses file system access to compute cryptographic hashes of files in user-designated folders and safely send duplicate files to the Windows Recycle Bin.

### 3. Local SQLite Database
CleanSnap creates a strictly local SQLite database (`cleansnap.db3`) stored exclusively in the app's sandboxed local data directory. This database stores:
- Cleanup history records (timestamp and bytes freed)
- An audit log of recycled files (to assist user review and recovery)
- Hash caching records (to accelerate subsequent scans)

No remote databases, servers, or cloud accounts are used.

### 4. Third-Party Services
CleanSnap uses **zero third-party tracking SDKs**, analytics libraries, or advertising networks.

### 5. Contact
For questions or bug reports, please open an issue on the official GitHub repository:
https://github.com/ATP0925/CleanSnap
