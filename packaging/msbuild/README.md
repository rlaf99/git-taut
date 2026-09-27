## How to build the nuget packages?

In the root directory, run `dotnet pack`.

## How to exeucte the ReinstallTools.targets?

Run `dotnet msbuild --verbosity:d path/to/ReinstallTools.targets`

## How to merge published files of git-remote-taut into that of git-taut?

Run `dotnet msbuild --verbosity:d -p:TargetFramework=net10.0 -p:RuntimeIdentifier=win-x64 path/to/MergePublished.targets`, (choosing TargetFramework and RuntimeIdentifier appropriately for your environment)

## How to build app images for git-remote-taut and git-taut?

To build for git-remote-taut, run `dotnet msbuild --verbosity:d path/to/BuildAppImages.targets -p:RuntimeIdentifier=linux-x64 -t:BuildGitRemoteTautAppImage`.

To build for git-taut, run `dotnet msbuild --verbosity:d path/to/BuildAppImages.targets -p:RuntimeIdentifier=linux-x64 -t:BuildGitTautAppImage`.