## dotnet tool

```
dotnet tool uninstall -g git-taut
dotnet tool install --global --add-source nupkg --prerelease git-taut
```

## test environment

Create repo1

```sh
git init --bare repo0
git clone repo0 repo1
cd repo1
echo repo1 > README
echo *.tt taut >> .gitattributes
echo tt taut >> .gitattributes
echo tt/** taut >> .gitattributes
echo *.zz taut compression-target-ratio=50 >> .gitattributes
echo *.dd taut delta-encoding-target-ratio=50 delta-encoding-enabling-size=80 >> .gitattributes
git add --all
git commit -m init
git push
```

Update repo1

```sh
echo .swp > .gitignore
git add --all
git commit -m "update .gitignore"
```

```cmd
set PATH=L:\Joyride\git-taut\Cli.Remote.Taut\bin\Debug\net10.0;%PATH%
set PATH=L:\Joyride\git-taut\Cli.Taut\bin\Debug\net10.0;%PATH%
set GIT_ASKPASS=L:\Joyride\git-taut\.vscode\git\getpass.sh
set GIT_CONFIG_SYSTEM=/dev/null
set GIT_CONFIG_GLOBAL=L:\Joyride\git-taut\.vscode\git\taut.gitconfig
set GIT_TAUT_TRACE=1
set GIT_TRACE=1
```

```
set GIT_SSH_COMMAND=git-taut dbg-ssh-bypass
```


```
set GIT_TAUT_LIST_FOR_PUSH_NO_FETCH=1
```

```mac
export PATH=$HOME/Joyride/git-taut/Cli.Remote.Taut/bin/Debug/net10.0:$PATH
export PATH=$HOME/Joyride/git-taut/Cli.Taut/bin/Debug/net10.0:$PATH
export GIT_ASKPASS=$HOME/Joyride/git-taut/Cli.Tests/scripts/getpass.sh
export GIT_CONFIG_SYSTEM=/dev/null
export GIT_CONFIG_GLOBAL=$HOME/Joyride/git-taut/bronly/git/taut.gitconfig
export GIT_TAUT_TRACE=1
export GIT_TRACE=1
```

```mac
export GIT_TAUT_LIST_FOR_PUSH_NO_FETCH=1
```

```cmd
git clone taut::./repo0 repo2
echo "*.tt taut" > .gitattributes
echo "*.taut taut" >> .gitattributes
```

```zsh
export PATH=$HOME/Joyride/git-taut/Cli.Taut/scripts/:$PATH
export PATH=$HOME/Joyride/git-taut/Cli.Taut/bin/Debug/net10.0:$PATH
export GIT_TAUT_TRACE=1
export GIT_TRACE=1
```

## Temp

- https://stackoverflow.com/questions/74791297/why-is-dotnet-test-command-line-not-respecting-run-settings
  - ```
    So, /p:settings=runsettings.runsettings is not a valid way to set parameters, anymore. I switched to --settings:"runsettings.runsettings", and now it all works.
    ```

## Tools

- msbuild
  `dotnet msbuild MyProject.csproj -t:Clean;MyCustomTarget`
- imdisk
  - `imdisk.exe -a -s 128M -m Q: -p "/fs:ntfs /q /v:ramdisk /y"`
- HelpViewer
  - `start "" "C:\Program Files (x86)\Microsoft Help Viewer\v2.3\HlpViewer.exe" /catalogName VisualStudio15 /locale en-us`
- ClangSharp
  - `https://github.com/dotnet/ClangSharp`
  - `dotnet tool install --global ClangSharpPInvokeGenerator --version 20.1.2`
  - however, additional manual steps are required
    - for libclang
      - run `dotnet fsi` and enter `#r "nuget: libclang.runtime.osx-arm64, 20.1.2";;` to download the nuget
      - copy `libclang.dylib` from `~/.nuget/packages/libclang.runtime.osx-arm64/20.1.2/runtimes/osx-arm64/native` to `~/.dotnet/tools/.store/clangsharppinvokegenerator/20.1.2/clangsharppinvokegenerator/20.1.2/tools/net8.0/any`
    - for libClangSharp
      - repeat the steps similar in that for libclang
- dotnet
  - How to publish single file
    - From <https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview?tabs=cli>, `dotnet publish -p:PublishSingleFile=true --self-contained false`.
  - Publish AOT
    - `dotnet publish -r linux-x64 -c Release`
    - `dotnet publish -r win-x64 -c Release`
    - `dotnet publish -r osx-arm64 -c Release`
- lmdb
  - Use UCRT64 environment to build lmdb. The `prefix` in the makefile will have to changed to a suitable location.
  - `mdb_dump -a .git\taut\objects\info\taut`
- Shakespare
  - https://shakespeare.mit.edu/Poetry/

## Submodules

- libgit2
  - `git fetch origin --update-shallow v1.9.1 && git reset --hard FETCH_HEAD`
  - this should match the libigt2 binary built into Libgit2Sharp.Native

## ideas

- Ideas
  - certain section is the commit message can be crypted if specified
  - provide a command to merge linked camps
- Questions
  - how to dispose userkeyholder?

## deprecation

### 251021 deprecation of ConsoleAppFramework

  - `Sys` was used to support a hidden command `--x-wait-all-spawned`
  - Why move from ConsoleAppFramework to System.CommandLine
    - There are things in ConsoleAppFramework that I am not used to
      -  `[arguments] comes before [options]`
      -  No global options, see `https://github.com/Cysharp/ConsoleAppFramework/issues/140`
   -  Plus, System.CommandLine is going to be stable and standard in .net world.
      -  However, System.CommandLine does not have dependency injection packaged, see <https://github.com/dotnet/command-line-api/issues/2576#issuecomment-3216688778>

## Project Logs

- 202511 Run tests in isolated process
  - it should be possible to use TUnit, see discussion here: <https://github.com/thomhurst/TUnit/discussions/1796>
- 202511 create a subcommand that runs kestrelserver
  - https://github.com/PeteX/StandaloneKestrel
  - https://exploding-kitten.com/2024/10-host-git-part-1
  - https://github.com/davidfowl/MultiProtocolAspNetCore