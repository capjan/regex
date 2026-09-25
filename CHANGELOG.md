# Changelog

## Unreleased

### Fixed

- `--max-count` and `--offset-width` with a non-numeric value now report `Option '--max-count' requires a whole number, but got 'abc'.` instead of naming the internal type `System.Nullable`1[System.Int32]`.

## 2.0.0

### Breaking

- Targets `net10.0` only. `net5.0` and `netcoreapp3.1` (both out of support) are no longer built.
- Command line parsing now uses `System.CommandLine`. Options and short forms are unchanged, but the help output looks different and invalid option values (e.g. `--max-count abc`) are now an error instead of being silently ignored.
- Errors (missing pattern, invalid regular expression, IO errors) now return exit code `1` instead of `0`.

### Fixed

- `-c` / `--case-sensitive` had no effect, the search was always case-insensitive.
- Line context of a match was wrong for long lines: the text before the match was unlimited and the text after it was cut at the wrong position.
- `--version` could fail when the assembly had a copyright but no company attribute.
- "File not found" messages are now written to stderr.
- Files without matches are no longer rewritten in replace mode.

### Changed

- Added unit and end-to-end tests (`regex.Tests`).
- CI runs on Linux, macOS and Windows with .NET 10, GitHub Actions updated, Dependabot enabled.
- The NuGet workflow packs explicitly (`dotnet pack`) and skips duplicates. Building no longer produces a package.
