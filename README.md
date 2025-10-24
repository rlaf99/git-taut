[中文](./README.zh-cn.md)

## git-taut (experimental)

*git-taut* is designed with a scenario in mind, that you want to store some notes in a git repository and keep those notes private.

The privacy is achieved through encryption. *git-taut* creates a shadow repository inside the original repository. Inside the shadow repository, files as well as their names are encrypted as specified. You push the shadow repository to remote and keep the original repository local to yourself.

The encryption uses [the cipher scheme](./docs/CipherScheme.md). Additionally, [delta-encoding](./docs/DeltaEnconding.md) and [content-compression](./docs/ContentCompression.md) are exploited.

*git-taut* functions as a Git remote-helper, thus most of the operations are transparent to normal git use.

## How to install

Currently, `git-taut` is provided as a dotnet tool on following platforms

- Windows x64
- Linux x64
- macOS arm64

To install it, some prerequeistes must be met:

- .NET SDK 9.0
- `Lg2.native` nuget package which provides the prebuilt libgit2 runtimes (see below)

### Install Lg2.native NuGet Package

`Lg2.native` is published at <https://github.com/rlaf99/libgit2/pkgs/nuget/Lg2.native>.

The nuget resitry has to be added before installing `Lg2.native`

```
dotnet nuget add source --username [GitHubUsername] --password [GitHubAccessToken] --name github_rlaf99 https://nuget.pkg.github.com/rlaf99/index.json
```

It should be sufficient for the `[GitHubAccessToken]` to only have package read permission.

Then `Lg2.native` can be successfully when resorting nuget packages.


### Install `git-taut` as a DotNet Tool 

In the root directory, run 

```
dotnet pack Cli
```

then the correpsonding nuget package is generated inside `nupkg` directory.

To install the nuget package, run

```
dotnet tool install --global --add-source nupkg git-taut
```