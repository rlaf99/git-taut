
## git-taut (experimental)

*git-taut* 的设计初衷是，你希望将一些笔记存储在git仓库中，并保持这些笔记的私密性。

私密性通过加密实现。*git-taut* 会在原始仓库内部创建一个影子仓库。在影子仓库中，文件及其名字都可以根据需要进行加密。你可以将影子仓库推送到远程，而将原始仓库保留在本地。

加密采用[此加密方案](./docs/CipherScheme.md)。此外，还利用了[增量编码](./docs/DeltaEnconding.md)和[内容压缩](./docs/ContentCompression.md)。

*git-taut*以Git remote-helper的形式工作，因此大多数操作对普通Git用户来说是透明的。