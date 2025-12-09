[中文](./README.zh-cn.md)

## git-taut (preview)

[git-taut] encrypts content in a Git repository through [Git Remote Helper](gitremote-helpers). 
The main scenario in mind when designing it is to enable storing confidential information in Git without revealing the content if you push it to remote.

Under the hood, **git-taut** mirrors the commit history of the original repository into a shadow repository called a **taut site** which resides in the original repository. 
In a taut site, files as well as their names are encrypted as specified (through `.gitattributes`). 
You push **taut sites** to remote and keep the original repository local to yourself.
The remote repository contains encrypted content and is called **tautened repository**.

An example **tautened repository** is available at [git-taut-example-tautened], whereas the corresponding original repository is at [git-taut-example-original].

The function of **git-taut** is provided to Git through a [Git Remote Helper](gitremote-helpers), thus most of the operations are transparent to Git.

The encryption uses [the cipher scheme](./docs/CipherScheme.md). 
Additionally, [delta-encoding](./docs/DeltaEnconding.md) and [content-compression](./docs/ContentCompression.md) are exploited.

## How to use

**git-taut** comes with two command line executables: `git-taut` and `git-remote-taut`, 
the former for managing **taut sites**, whereas the latter for working with Git as a remote helper.

### Cloning from a tautened repository

Suppose you have an already tautened repository,
to regain the original repository out of it, 
all you have to do is clone it with its url prefixed with `taut::`:

```
git clone taut::url/to/tautened regained
```

Git then automatically invokes `git-remote-taut` to handle the underlying details.

> If you want to start a new repository, just clone from an empty **tautened repository**.

### Adding a tautened repository

It is also possible to use `git-taut` to add a **tautened repository** to a local repository:

```
git taut add name url/to/tautened
```

The above adds a new **taut site** for the local repository corresponding to remote whose name is `name` and url is `taut::url/to/tautened`.

> You can add an empty **tautened repository** as **taut site**, then populate it by pushing the local Git content to it.

### Setting username and password

Setting up a **taut site** requires you to provide username (optional) and password.
The username is optional, and used as salt when generating encryption keys.

`git-taut` utilizes Git credential helper for retrieving them.
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

If a `.gitattribute` contains the following setting, 
then files with extension `.a_file` in this directory as well as sub-directories will be encrypted.

```
*.a_file taut
```

The above set `taut` attribute on all files with extension `.a_file`.

#### Example 2: Ecrypting filers under directory `a_dir`

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

**git-taut** is available on the following platforms

- Windows x64
- Linux x64
- macOS arm64

Currently, only development preview is available, see the [Development setup](#development-setup) section below.

## Development setup

### Build and install **git-taut** executables locally

Clone the source, and run `dotnet pack` in the root directory.
The NuGet packages for `git-taut` and `git-remote-taut` are produced at `nupkg` directory.
They can be installed through `dotnet-cli`:

```
dotnet tool install --global --source nupkg --prerelease git-taut
dotnet tool install --global --source nupkg --prerelease git-remote-taut
```

### Development version of `Lg2.native` NuGet package

`Lg2.native` provides [libgit2] binaries for .NET and published at <https://www.nuget.org/packages/Lg2.native>.
However, development version of `Lg2.native` can be published at <https://github.com/rlaf99/libgit2/pkgs/nuget/Lg2.native>.

For development version, the nuget resitry has to be added before installing `Lg2.native`

```
dotnet nuget add source --username [GitHubUsername] --password [GitHubAccessToken] --store-password-in-clear-text --name github_rlaf99 https://nuget.pkg.github.com/rlaf99/index.json
```

It should be sufficient for the `[GitHubAccessToken]` to only have package read permission.

[gitattributes]: https://git-scm.com/docs/gitattributes
[gitremote-helpers]: https://git-scm.com/docs/gitremote-helpers
[libgit2]: https://github.com/rlaf99/libgit2/
[git-taut]: https://github.com/rlaf99/git-taut
[git-taut-example-tautened]: https://github.com/rlaf99/git-taut-example-tautened
[git-taut-example-original]: https://github.com/rlaf99/git-taut-example-original