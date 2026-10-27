# Windows preview build

2026-10-27 · editor-tooling-codex

- Unity 6000.5.6f1, Windows x64, non-development, LZ4 + strict mode.
- Source HEAD: 619fbe885624445b5bffba0be1dbf1c0497c1e4a. Existing uncommitted presentation/settings assets were preserved and included from the working tree; this is not a clean-commit reproducibility claim.
- Projectile package: d88c01fbc64555ae0bdce0b8fd2b4041781da39e.
- Explicit scenes: MainMenu, MilitaryAI, MilitaryMatch. SampleScene excluded.
- Build succeeded: 319.124 seconds, 115.44 MB, 0 errors / 3 warnings. Existing obsolete Unity lookup API warnings; MCP command timeouts occurred while the editor was busy compiling.
- ZIP: Builds/Downloads/ProjectF-Windows-0.1.0-preview.zip, 57,171,233 bytes.
- SHA256: 0c4b6dcb2b8cbd4c93ac539ceccb7ddd089a18e27107268c1e7edc54555723d1.
- Packaging includes generated runtime, HTML instructions, seven license notices, release manifest. Raw source/models and DoNotShip/debug symbols excluded.
- Standalone launch smoke: Direct3D 12 device and input initialized, player data loaded, no managed exceptions in player log. Driver debug-info query warning did not prevent initialization. Full standalone match interaction was not tested; related Editor PlayMode 32/32 passed before packaging.
- Fortress.dll and UnityProjectile.Runtime.dll present, no Editor/test assemblies found in inspected matching names. MCPForUnity.Runtime.dll helper assembly is included; reviewer found no auto-starting server in its runtime source.
- Architecture and independent source/packaging review: no blocking findings at review time. Reviewer used inherited model because protocol's Opus model is unavailable in this harness.
- Website publication is pending the user's 별빛공방 URL/repository/hosting information. No public release or site modification has occurred.

Package again with `scripts/package-windows.ps1` after a successful build, using a new version when an existing output is present. Binary output remains ignored by Git.
