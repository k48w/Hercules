# Hercules audit

Audit date: 2026-06-22

## Completed

- Rebranded product namespaces, assembly, solution, project, executable, themes, assets, UI text, local data paths, and registry identifiers from Voidstrap to Hercules.
- Preserved upstream Bloxstrap, Fishstrap, WPF UI, and third-party attribution.
- Removed a machine-specific `DiscordRPC.dll` reference; the existing NuGet dependency is now the single source.
- Added an explicit Newtonsoft.Json dependency for source files that previously relied on an accidental transitive package reference.
- Upgraded SharpCompress to 0.49.1 to remove the vulnerable 0.47.0 dependency and removed redundant framework package references.
- Added the missing persisted SwiftTunnel settings that previously caused 19 compilation errors.
- Removed repeated MSBuild resource declarations and corrected the packaged license path.
- Stopped embedding developer computer and user names in release metadata and user-agent strings.
- Aligned the target framework, manifest, runtime guard, and publish profiles on Windows 10 version 1809 or newer.
- Replaced machine-specific publish folders with repository-relative output paths.
- Fixed duplicate `MusicPlayerViewModel` definitions that prevented compilation and normalized source filenames containing trailing spaces.
- Consolidated duplicated GitHub release/asset models shared by the Hub and Releases pages.
- Hardened automatic updates: HTTPS only, bounded download size, exact asset name, release tag consistency, mandatory SHA-256 verification, rollback on replacement failure, and a single restart path.
- Corrected the malformed GitHub repository identifier that produced double slashes in API URLs.
- Replaced the unrelated contribution-animation workflow with Windows CI that restores and builds Hercules using .NET 10.

## Release blockers

- Configure the trusted GitHub owner in `Hercules/App.xaml.cs`; updates must remain disabled while the placeholder is present.
- Install the .NET 10 SDK and run restore/build/publish. This workstation currently has only the .NET host and cannot compile the solution.
- Replace the inherited Hercules artwork if a new visual identity is desired; filenames and resource identity are already migrated.

## Follow-up recommendations

- Extend CI with unit tests and signed release publishing once release credentials are configured.
- Sign `Hercules.exe` with an Authenticode certificate in addition to SHA-256 release verification.
- Add unit tests around version comparison, path validation, JSON migrations, and updater rejection cases.
- Gradually replace broad empty `catch` blocks with scoped exception handling and structured logging.
- Review optional integrations that execute external programs or modify GPU/Roblox settings under a least-privilege policy.
