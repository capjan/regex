Output examples/videos/named-replacement.gif
Output examples/videos/named-replacement.mp4
Require dotnet

Set Shell bash
Set FontSize 22
Set Width 1200
Set Height 520
Set Padding 24
Set Margin 16
Set MarginFill "#101522"
Set BorderRadius 12
Set WindowBar Colorful
Set Theme "Catppuccin Mocha"
Set TypingSpeed 45ms

# Build quietly, prepare a fresh writable copy, then expose the apphost.
Hide
Type "dotnet build regex/regex.csproj -c Release --nologo >/dev/null && mkdir -p examples/videos/.work && cp examples/data/names.txt examples/videos/.work/names.txt && alias regex=./regex/bin/Release/net10.0/regex && clear"
Enter
Sleep 1s
Show

Type "regex 'Name:(?<name>[A-Za-z]+)' --replace 'id=$''{name}' --verbose examples/videos/.work/names.txt"
Enter
Sleep 2s
Type "cat examples/videos/.work/names.txt"
Enter
Sleep 3s

Hide
Type "rm -f examples/videos/.work/names.txt && rmdir examples/videos/.work"
Enter
