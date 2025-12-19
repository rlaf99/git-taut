## How to build the nuget packages?

In the parent directory, run `dotnet build pack`.

## How to exeucte the ReinstallTools.targets?

In the directory, run `dotnet msbuild --verbosity:d ReinstallTools.targets`

## How to merge published files of git-remote-taut into that of git-taut?

In the directory, run `dotnet msbuild --verbosity:d -p:TargetFramework=net10.0 -p:RuntimeIdentifier=win-x64 msbuild\MergePublished.targets`, (choosing TargetFramework and RuntimeIdentifier appropriately for your environment)
