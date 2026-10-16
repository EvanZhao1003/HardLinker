# HardLinker

[English](README.md) | [简体中文](README.zh-CN.md)

一个轻量的 Windows WPF 文件硬链接工具。

## 功能

- **文件模式：** 选择多个源文件，将它们硬链接到已存在的目标目录。
- **文件夹模式：** 将源文件夹第一层的普通文件硬链接到新建的目标文件夹中，不递归处理子文件夹。
- 可配置默认源目录和目标目录。两种模式都会在最近使用的路径不可用时回退到默认目录。文件选择器会从最近使用的源目录或默认源目录打开。
- 自动记住最近使用的路径和上次使用的文件夹名称。还可以保存最多 8 个常用文件目标目录和 8 个常用目标文件夹名称。
- 界面支持英语和中文，默认使用英语。
- 不会覆盖已存在的目标名称，并逐项报告成功和失败结果。

设置保存在 `%AppData%\HardLinker\settings.json`。

硬链接只能在同一卷内创建。硬链接共享同一份文件数据，因此通过任一链接修改内容，其他链接也会看到修改。

## 构建和运行

安装 .NET 8 SDK 和 Windows Desktop Runtime，然后运行：

```powershell
dotnet build .\HardLinker.csproj
dotnet run --project .\HardLinker.csproj
```
