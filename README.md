[![.NET](https://github.com/capjan/regex/actions/workflows/dotnet.yml/badge.svg)](https://github.com/capjan/regex/actions/workflows/dotnet.yml)
[![Coverage](https://raw.githubusercontent.com/capjan/regex/badges/coverage.svg)](https://github.com/capjan/regex/actions/workflows/dotnet.yml)

# regex

Search and replace with **.NET regular expressions** from the command line, using the same engine as your C# code.

<img src="assets/hero-regex.png" alt="regex CLI and C# share the .NET regular expression engine: same pattern, same engine">

## Why not grep or ripgrep?

Use them for general text search, they are faster and more widely installed. Use `regex` when you need the .NET flavor:

- Try a pattern on the CLI, then paste it into C# unchanged. No dialect differences.
- .NET-only features such as balancing groups, and named groups in replacements (`--replace "id=${name}"`).
- Lookarounds and backreferences without extra flags or a separate engine.
- One tool with the same behavior on Linux, macOS and Windows, installed via `dotnet tool`.

## Features

- .NET Regular Expressions for CLI on Linux, macOS and Windows
- [.NET Tool](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools) Deployment
- [NuGet Package](https://www.nuget.org/packages/cap.regex/)
- Permissive [MIT License](./LICENSE)

## Install

Requires the [.NET 10](https://dotnet.microsoft.com/download) SDK or runtime.

```
dotnet tool install --global cap.regex
```

Update to the latest version:

```
dotnet tool update --global cap.regex
```

## Options

```
Usage:

  regex [<pattern> [<path>...]] [options]

Arguments:
  <pattern>  The search pattern as .NET Regular Expression (RegEx).
  <path>     File or directory to operate. (A directory must end with a directory separator)

Options:
  -R, --replace <replace>        replacement Pattern (regex)
  -n, --dry-run                  with --replace: report what would change, but do not write any file
  -d, --diff                     with --replace: print the changes as unified diff, but do not write any file
  -c, --case-sensitive           enables case-sensitive behavior - btw. disables the by default enabled ignore-case option
  -f, --filter <filter>          wildcard based file filter, e.g. *.txt [default: *.*]
  -r, --recursive                progress all subdirectories
  --offset-width <offset-width>  output-formatting: set the count of characters used for the offset column [default: 6]
  -o, --only-matching            prints only the match
  -m, --max-count <max-count>    limit matches to the given count
  -v, --verbose                  show additional information
  -V, --version                  show version information
  -?, -h, --help                 Show help and usage information
```

Exit code is `0` on success and `1` on errors (e.g. missing pattern, invalid regular expression or invalid option value).

## Usage Examples

Every example is self-contained: the files it starts from, the command, and the exact output. Click to expand.

<details>
<summary><b>Search a directory tree</b> for a word in <code>*.txt</code> files</summary>

<br>

Starting point:

```
docs/
├── intro.txt        "Hello World" / "hello again" / "Goodbye"
└── sub/
    ├── more.txt     "Say Hello to everyone" / "nothing here"
    └── skip.md      "no match"
```

Command (`--recursive` walks subfolders, `--filter` limits the files, a directory must end with `/`):

```
$ regex --recursive --filter '*.txt' Hello docs/
Offset:0      Hello World
Offset:12     hello again
docs/intro.txt: found 2 matches
Offset:4      Say Hello to everyone
docs/sub/more.txt: found 1 match
```

Matching is case-insensitive by default, so `hello again` is found too. The offset is the character position of the match in the file. `skip.md` is ignored by the filter.

</details>

<details>
<summary><b>Case-sensitive search</b> with <code>--case-sensitive</code></summary>

<br>

`notes.txt`:

```
Hello there
hello there
```

Default (case-insensitive) versus `--case-sensitive`:

```
$ regex Hello notes.txt
Offset:0      Hello there
Offset:12     hello there
notes.txt: found 2 matches

$ regex --case-sensitive Hello notes.txt
Offset:0      Hello there
notes.txt: found 1 match
```

</details>

<details>
<summary><b>Print only the match</b> with <code>--only-matching</code> and <code>--max-count</code></summary>

<br>

`names.txt`:

```
Name:Alice
Name:Bob
Age:42
```

Print just the matched text instead of the whole line:

```
$ regex --only-matching 'Name:\w+' names.txt
Name:Alice
Name:Bob
```

Stop after the first match:

```
$ regex --max-count 1 --only-matching 'Name:\w+' names.txt
Name:Alice
```

</details>

<details>
<summary><b>Replace with a named group</b> (rewrites the file)</summary>

<br>

`names.txt` before:

```
Name:Alice
Name:Bob
Age:42
```

Command. Use single quotes in bash/zsh so the shell does not expand `${name}`:

```
$ regex 'Name:(?<name>[A-Za-z]+)' --replace 'Hello ${name}, how are you?' names.txt
names.txt: did 2 replacements
```

`names.txt` after:

```
Hello Alice, how are you?
Hello Bob, how are you?
Age:42
```

Files without matches are left untouched.

</details>

<details>
<summary><b>Preview a replacement</b> with <code>--dry-run</code> (counts only)</summary>

<br>

`--dry-run` reports what would change and writes nothing. It also works across a whole tree.

`docs/intro.txt` contains 2 matches for `hello`, `docs/sub/more.txt` contains 1:

```
$ regex --recursive --filter '*.txt' --dry-run hello --replace bye docs/
docs/intro.txt: would do 2 replacements
docs/sub/more.txt: would do 1 replacement
```

Both files are unchanged afterwards.

</details>

<details>
<summary><b>Preview as unified diff</b> with <code>--diff</code>, then apply it with <code>patch</code></summary>

<br>

`names.txt`:

```
Name:Alice
Name:Bob
Age:42
```

`--diff` prints the changes as a unified diff and writes nothing:

```
$ regex 'Name:(?<name>[A-Za-z]+)' --replace 'id=${name}' --diff names.txt
--- names.txt
+++ names.txt
@@ -1,3 +1,3 @@
-Name:Alice
-Name:Bob
+id=Alice
+id=Bob
 Age:42
```

The diff goes to stdout without colors when redirected, so you can save it, review it and apply it later:

```
$ regex 'Name:(?<name>[A-Za-z]+)' --replace 'id=${name}' --diff names.txt > names.patch
$ patch -p0 < names.patch
patching file names.txt
```

`names.txt` after `patch`:

```
id=Alice
id=Bob
Age:42
```

</details>

## Changelog

See [CHANGELOG.md](./CHANGELOG.md).
