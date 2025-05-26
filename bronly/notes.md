
## test environment

Create repo1

```sh
git init repo1
cd repo1
echo repo1 > README
git add --all
git commit -m init
```

Update repo1

```sh
echo .swp > .gitignore
git add --all
git commit -m "update .gitignore"
```

```cmd
set PATH=L:\Joyride\git-remote-taut\Cli\bin\Debug\net9.0;%PATH%
set GIT_REMOTE_TAUT_TRACE=1
set GIT_TRACE=1
```

## Tools

- imdisk
  - `imdisk.exe -a -s 128M -m Q: -p "/fs:ntfs /q /v:ramdisk /y"`
- ClangSharp
  - `https://github.com/dotnet/ClangSharp`
  - `dotnet tool install --global ClangSharpPInvokeGenerator --version 20.1.2`
  - however, additional manual steps are required
    - for libclang
      - run `dotnet fsi` and enter `#r "nuget: libclang.runtime.osx-arm64, 20.1.2";;` to download the nuget
      - copy `libclang.dylib` from `~/.nuget/packages/libclang.runtime.osx-arm64/20.1.2/runtimes/osx-arm64/native` to `~/.dotnet/tools/.store/clangsharppinvokegenerator/20.1.2/clangsharppinvokegenerator/20.1.2/tools/net8.0/any`
    - for libClangSharp
      - repeat the steps similar in that for libclang

## Submodules

- libgit2
  - `git fetch origin --update-shallow v1.8.4 && git reset --hard FETCH_HEAD`
  - this should match the libigt2 binary built into Libgit2Sharp.Native