
## How to build libgit2

### Windows

Prerequisite: CMake and Visual Studio (e.g., community version) are installed.

Steps:

1. `git clone --depth=1 ../libgit2 libgit2`

1. `cd libgit2`

1. `git apply ../patches/01.patch`

1. `cd .. && md build && cd build`

1. `md Debug && cd Debug`

1.  Configure Debug build
    ```
    cmake -DBUILD_TESTS=OFF ^
        -DBUILD_CLI=OFF ^
        -DUSE_SSH=exec ^
        -DUSE_HTTPS=OFF ^
        -DREGEX_BACKEND=builtin ^
        -DUSE_BUNDLED_ZLIB=ON ^
        -DLIBGIT2_FILENAME=git2 ^
        ../../libgit2
    ```
1. Make Debug build and install
  - `cmake --build . --config Debug`
  - `cmake --install . --config Debug --prefix ../../install/Debug`

1. `cd .. && md Release && cd Release`

1.  Configure Release build
    ```
    cmake -DBUILD_TESTS=OFF ^
        -DBUILD_CLI=OFF ^
        -DUSE_SSH=exec ^
        -DUSE_HTTPS=OFF ^
        -DREGEX_BACKEND=builtin ^
        -DUSE_BUNDLED_ZLIB=ON ^
        -DLIBGIT2_FILENAME=git2 ^
        ../../libgit2
    ```

1. Make Release build and install
  - `cmake --build . --config Release`
  - `cmake --install . --config Release --prefix ../../install/Release`


### macOS

Prerequisite: CMake and XCode are installed.

Steps:

1. `git clone --depth=1 ../libgit2 libgit2`

1. `cd libgit2`

1. `git apply ../patches/01.patch`

1. `cd .. && mkdir build && cd build`

1. `mkdir Debug && cd Debug`

1. Configure Debug build
    ```
    cmake -DBUILD_TESTS=OFF \
        -DBUILD_CLI=OFF \
        -DUSE_SSH=exec \
        -DUSE_HTTPS=OFF \
        -DREGEX_BACKEND=builtin \
        -DUSE_BUNDLED_ZLIB=ON \
        -DLIBGIT2_FILENAME=git2 \
        -DCMAKE_BUILD_TYPE=Debug \
        ../../libgit2
    ```
1. Make Debug build and install
  - `cmake --build . --config Debug`
  - `cmake --install . --config Debug --prefix ../../install/Debug`

1. `cd .. && mkdir Release && cd Release`

1. Configure Release build
    ```
    cmake -DBUILD_TESTS=OFF \
        -DBUILD_CLI=OFF \
        -DUSE_SSH=exec \
        -DUSE_HTTPS=OFF \
        -DREGEX_BACKEND=builtin \
        -DUSE_BUNDLED_ZLIB=ON \
        -DLIBGIT2_FILENAME=git2 \
        -DCMAKE_BUILD_TYPE=Release \
        ../../libgit2
    ```

1. Make Release build and install
     - `cmake --build . --config Release`
     - `cmake --install . --config Release --prefix ../../install/Release`

## About the patches

When applying to libgit2, the patch is subject to libgit2's license <https://github.com/libgit2/libgit2?tab=readme-ov-file#license>.
