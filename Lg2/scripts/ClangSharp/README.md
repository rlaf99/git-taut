## How to generate libgit2 bindings

<https://github.com/dotnet/ClangSharp> should be installed.

The bindings are generated with one single command (run at the project root directory): 

```clangsharppinvokegenerator @Lg2\scripts\ClangSharp\libgit2.respfile```

However, the libgit2 source has to be prepared, that is, cloned into `\thirdparty\libgit2build\libgit2` and patched.
See `\thirdparty\libgit2build\README.md` for more information.