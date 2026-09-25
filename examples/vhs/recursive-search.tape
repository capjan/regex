Output examples/videos/recursive-search.gif
Output examples/videos/recursive-search.mp4
Require dotnet

Set Shell bash
Set FontSize 22
Set Width 1200
Set Height 480
Set Padding 24
Set Margin 16
Set MarginFill "#101522"
Set BorderRadius 12
Set WindowBar Colorful
Set Theme "Catppuccin Mocha"
Set TypingSpeed 45ms

# Build the CLI quietly and add its apphost to this recording's PATH.
Hide
Type "dotnet build regex/regex.csproj -c Release --nologo >/dev/null && alias regex=./regex/bin/Release/net10.0/regex && clear"
Enter
Sleep 1s
Show

Type "regex --recursive --filter '*.txt' Hello examples/data/recursive/"
Enter
Sleep 3s
