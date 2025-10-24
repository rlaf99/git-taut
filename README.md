[中文](./README.zh-cn.md)

## git-taut (experimental)

*git-taut* is designed with a scenario in mind, that you want to store some notes in a git repository and keep those notes private.

The privacy is achieved through encryption. *git-taut* creates a shadow repository inside the original repository. Inside the shadow repository, files as well as their names are encrypted as specified. You push the shadow repository to remote and keep the original repository local to yourself.

The encryption uses [the cipher scheme](./docs/CipherScheme.md). Additionally, [delta-encoding](./docs/DeltaEnconding.md) and [content-compression](./docs/ContentCompression.md) are exploited.

*git-taut* functions as a Git remote-helper, thus most of the operations are transparent to normal git use.

### Install Lg2.native nuget

`Lg2.native` nuget is published at <https://github.com/rlaf99/libgit2/pkgs/nuget/Lg2.native>.

The nuget resitry has to be added before installing `Lg2.native`

```
dotnet nuget add source --username [YourGitHubUsername] --password [YourPAT] --name github_rlaf99 https://nuget.pkg.github.com/rlaf99/index.json
```

Then `Lg2.native` can be installed as usual (e.g., `dotnet package add Lg2.native`), and successfully restored.