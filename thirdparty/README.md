
- libgit2
  - ClangSharp is used to scan the headers and generate bindings.
- lmdb
  - Sources for the command line utilities for lmdb data inspection.

## How to build libgit2

```
mkdir ../build
git clone libgit2 ../build/libgit2
git apply --directory=../build/libgit2 libgit2patches/01.patch 
```