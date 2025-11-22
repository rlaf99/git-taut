
## How to setup testbed

Cli.Tests requires a testbed which is a directory to store temporary files generated during testing.

By default testbed is `Testbed` directory under Cli.Tests.
To improve performance, you could mount a ramdisk on to that directory.

On Windows, [ImDisk] is a tool to create and initialize ramkdisk as testbed:

```
sudo imdisk.exe -a -s 128M -m path/to/Testbed -p "/fs:ntfs /q /v:ramdisk /y"
```

> The above assumes that you have `sudo` installed on Windows, otherwise you need to run it (without "sudo" in the front) in with  administrator priviledge. 

On macOS, the script [mkramdisk.macos.sh] can assist in ramdisk creation:

```
mkramdisk.macos.sh 262144 /path/to/Testbed
```

[mkramdisk.macos.sh]: ./scripts/mkramdisk.macos.sh
[ImDisk]: https://github.com/LTRData/ImDisk