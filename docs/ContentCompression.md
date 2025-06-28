
## Content Compression

To find out whether or not to store content as compressed, *git-taut* first compresses the content and if the size of the compressed content is below a percentage (roughly 80%) of the size of the original content, the content is stored as compressed, otherwise the content is stored as it is.

Content compression uses Zstd.