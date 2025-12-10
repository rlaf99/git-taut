## git-taut（预览版）

[git-taut]通过[Git Remote Helper](gitremote-helpers)对 Git 仓库中的内容进行加密。
设计初衷是将一些机密信息存储在Git仓库中，确保在推送到远程仓库时内容不会泄露。

机制上，**git-taut**将原始仓库的commit历史镜像到叫做**taut site**的影子仓库中，该影子仓库位于原始仓库内部。在 **taut site** 中，（通过 .gitattributes）指定的文件内容及其名称都会被加密。推送到远程的是**taut site**，原始仓库保留在本地。包含加密内容的远程仓库被称为**tautened仓库**。

[git-taut-example-tautened]是一个**tautened仓库**的示例, 相应的原始仓库在[git-taut-example-original]。

**git-taut**的功能是通过[Git Remote Helper](gitremote-helpers)提供给 Git 的，因此大多数操作对 Git而言都是透明的。

加密采用[此加密方案](./docs/CipherScheme.md)。此外，还利用了[增量编码](./docs/DeltaEnconding.md)和[内容压缩](./docs/ContentCompression.md)，等等。

## 如何使用

**git-taut**有两个命令行工具: `git-taut` 和 `git-remote-taut`, 
前者用来管理**taut sites**, 后者作为Remote Helper被Git调用.

### 从tautened仓库克隆

假设已有一个tautened仓库，要从中获得原始仓库，只须克隆仓库的时候在其URL前面加上前缀`taut::`:

```
git clone taut::url/to/tautened regained
```

Git会自动调用`git-remote-taut`来处理底层的细节。

> 如果要开始一个新的仓库，可以克隆一个空的**tautened仓库**.

### 添加一个tautened仓库

可以使用`git-taut`来添加一个**tautened仓库**到本地仓库：

```
git taut add name url/to/tautened
```

上面添加一个新的**taut site**到本地仓库，配置中对应的remote名字是`name`、地址是`taut::url/to/tautened`.

> 可以添加一个空的**tautened仓库**来作为**taut site**, 然后把本地仓库的内容推送进去

### 设置用户名和密码


设置一个新的**taut site**需要提供用户名（可选）和密码。可选的用户名在生成加密密钥的时候作为salt。

`git-taut`采用Git Credential Helper来获取用户名和密码，会以**taut site**的URL作为提示。

### 决定哪些文件加密

`git-taut`依赖于[gitattributes]来决定哪些内容需要加密。
具有`taut`属性的目录和文件会被加密。

可以使用`git-check-attr`来检查某个路径是否设置有`taut`属性：

```
git check-attr some/path
```

`taut`设置了的情况下，会输出：

```
"some/path": taut: set
```

#### 例子 1：加密所有`.a_file`为后缀的文件

如果`.gitattribute`包含下面的设置，那么具有`.a_file`后缀的文件会被加密：

```
*.a_file taut
```

#### 例子 2：加密目录`a_dir`下的所有文件

对应的`.gitattribute`设置：

```
a_dir/** taut
```

上述会为所有位于`a_dir`路径之下的内容设置`taut`属性。可是，`taut`属性不会设置到`a_dir`目录本身。如果需要为`a_dir`设上`taut`属性，需要添加一行：

```
a_dir taut
```

## 如何安装

**git-taut**当前支持以下平台：

- Windows x64
- Linux x64
- macOS arm64

目前只提供开发预览版，可以参考下面的[开发环境搭建](#开发环境搭建)章节.

## 开发环境搭建

### 在本地构建并安装**git-taut**命令

克隆原代码，并在项目根目录下执行`dotnet pack`。
`git-taut` 和 `git-remote-taut`的NuGet包会生成在`nupkg`目录，然后可以使用`dotnet-cli`来安装：

```
dotnet tool install --global --source nupkg --prerelease git-taut
dotnet tool install --global --source nupkg --prerelease git-remote-taut
```

### `Lg2.native`NuGet包的开发版本

`Lg2.native` 提供了 [libgit2] 的编译后的二进制文件，正式版本发布在 <https://www.nuget.org/packages/Lg2.native>。
然后`Lg2.native`的开发版本发布在<https://github.com/rlaf99/libgit2/pkgs/nuget/Lg2.native>.

安装`Lg2.native`的开发版本需要添加GitHub的NuGet源地址：

```
dotnet nuget add source --username [GitHubUsername] --password [GitHubAccessToken] --store-password-in-clear-text --name github_rlaf99 https://nuget.pkg.github.com/rlaf99/index.json
```

将上面的`[GitHubAccessToken]`替换成你的GitHub访问号牌，具有可读权限就行。

[gitattributes]: https://git-scm.com/docs/gitattributes
[gitremote-helpers]: https://git-scm.com/docs/gitremote-helpers
[libgit2]: https://github.com/rlaf99/libgit2/
[git-taut]: https://github.com/rlaf99/git-taut
[git-taut-example-tautened]: https://github.com/rlaf99/git-taut-example-tautened
[git-taut-example-original]: https://github.com/rlaf99/git-taut-example-original
