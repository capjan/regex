# Example recordings

The VHS tapes in this directory reproduce the examples shown in the main README. Run them from the repository root with VHS and the .NET 10 SDK installed:

```sh
vhs examples/vhs/recursive-search.tape
vhs examples/vhs/case-sensitive.tape
vhs examples/vhs/named-replacement.tape
```

Each tape writes a GIF and an MP4 to `examples/videos/`. The replacement tape starts from `examples/data/names.txt` each time and removes its temporary working copy when the recording finishes.
