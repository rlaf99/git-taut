[中文](./README.zh-cn.md)

## git-taut (experimental)

*git-taut* is designed with a scenario in mind, that you want to store some notes in a git repository and keep those notes private.

The privacy is achieved through encryption. *git-taut* creates a shadow repository inside the original repository. Inside the shadow repository, files as well as their names are encrypted as specified. You push the shadow repository to remote and keep the original repository local to yourself.

The encryption uses [the cipher scheme](./docs/CipherScheme.md). Additionally, [delta-encoding](./docs/DeltaEnconding.md) and [content-compression](./docs/ContentCompression.md) are exploited.

*git-taut* functions as a Git remote-helper, thus most of the operations are transparent to normal git use.

## Under the hood

T.B.D.

## Usage

### Setting username and password

Setting up a *taut site* requires you to provide username (optional) and password.
The username is optional, and used as salt when generating encryption keys.
`git-taut` utilites Git credential helper for retrieving them.
The prompt will show the location of the *taut site* as URL.

### Deciding what files to encrypt

`git-taut` relies on [gitattributes] to tell what should be encrypted.
Directories and files with `taut` attribute set will be encrypted by `git-taut`.

`git-check-attr` can be used to check whehter `taut` attribute is set for a path:

```
git check-attr some/path
```

If `taut` attribute is set, then the output shows

```
"some/path": taut: set
```

#### Example 1: Encrypting files with extension `.a_file`

If a `.gitattribute` contains the following setting, then files with extension `.a_file` in this directory as well as sub-directories will be encrypted.

```
*.a_file taut
```

The above set `taut` attribute on all files with extension `.a_file`.

#### Exampl 2: Ecrypting filers under directory `a_dir`

To encrypt all files under folder `a_dir`, ensure corresponding `.gitattribute` has the following setting

```
a_dir/** taut
```

The above sets `taut` attribute on all content under `a_dir`. However, it does not cause the name of `a_dir` to be encrypted. 
Use the following setting To achieve it

```
a_dir taut
```

The above set `taut` attribute on `a_dir` itself.


## How to install

Currently, `git-taut` is provided as a dotnet tool for the following platforms

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
dotnet nuget add source --username [GitHubUsername] --password [GitHubAccessToken] --store-password-in-clear-text --name github_rlaf99 https://nuget.pkg.github.com/rlaf99/index.json
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
dotnet tool install --global --add-source nupkg --prerelease git-taut
```

And don't forget to copy [git-remote-taut] to a directory in PATH, otherwse Git does not know how to invoke the remote helper provided by git-taut.

[git-remote-taut]: ./Cli/scripts/git-remote-taut
[gitattributes]: https://git-scm.com/docs/gitattributes