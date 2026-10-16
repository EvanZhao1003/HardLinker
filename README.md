# HardLinker

[English](README.md) | [简体中文](README.zh-CN.md)

A lightweight Windows WPF utility for creating file hard links.

## Features

- **File mode:** Select multiple source files and link them into an existing destination folder.
- **Folder mode:** Link regular files from the top level of a source folder into a newly created destination folder. Subfolders are not processed recursively.
- Configure default source and destination folders. Both modes fall back to these folders when their recent paths are no longer available. The file picker opens at the recent source folder or the configured default source folder.
- Automatically remember recent paths and the last folder name. You can also save up to eight frequent file destination folders and eight frequent destination folder names.
- Switch the interface between English and Chinese. English is the default language.
- Existing destination names are never overwritten. The app reports successes and failures for each item.

Settings are stored in `%AppData%\HardLinker\settings.json`.

Hard links can only be created on the same volume. They share the same file data, so changes made through one link are visible through the others.

## Build and run

Install the .NET 8 SDK and Windows Desktop Runtime, then run:

```powershell
dotnet build .\HardLinker.csproj
dotnet run --project .\HardLinker.csproj
```
