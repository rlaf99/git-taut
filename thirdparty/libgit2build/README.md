
## How to build libgit2

### Windows

Prerequisite: CMake and Visual Studio (e.g., community version) are installed.

Steps:

1. `git clone --depth=1 ../libgit2 libgit2`

1. `cd libgit2`

1. `git apply ../patches/01.patch`

1. `cd ..`

1. `md build && cd build`

1.  ```
    cmake -DBUILD_TESTS=OFF ^
        -DBUILD_CLI=OFF ^
        -DUSE_SSH=exec ^
        -DUSE_HTTPS=OFF ^
        -DREGEX_BACKEND=builtin ^
        -DUSE_BUNDLED_ZLIB=ON ^
        -DLIBGIT2_FILENAME=libgit2 ^
        ../libgit2
    ```
1. Build & install:
   - Debug
     - `cmake --build . --config Release`
     - `cmake --install . --config Release --prefix ../install/Release`
   - Debug
     - `cmake --build . --config Debug`
     - `cmake --install . --config Debug --prefix ../install/Debug`

## About the patches

When applying to libgit2, the patch is subject to libgit2's license <https://github.com/libgit2/libgit2?tab=readme-ov-file#license>.
