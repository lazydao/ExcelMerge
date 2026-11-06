# ExcelMerge 便携修复版使用说明

此版本修复部分 `.xls` 文件在 SVN diff 时因缺少 BIFF 行记录而崩溃的问题。
读取时使用 NPOI 2.4.1，并在读取完成后释放工作簿文件句柄。

## 使用

1. 将压缩包完整解压到固定目录，例如 `D:\Tools\ExcelMerge`。
2. 保留目录内全部 DLL、配置文件及语言子目录。
3. 双击 `ExcelMerge.GUI.exe`，选择两份文件及对应工作表，点击 `Extract Diff`。

需要 Windows 和 .NET Framework 4.5.2 或兼容的更高版本。
运行时无需安装 Microsoft Excel，也不需要安装开发工具。

## 配置 TortoiseSVN

打开 `TortoiseSVN → Settings → Diff Viewer → Advanced`。
为 `.xls` 和 `.xlsx` 分别添加或修改外部差异工具命令：

```text
"D:\Tools\ExcelMerge\ExcelMerge.GUI.exe" diff -s %base -d %mine
```

将示例中的程序路径改为实际解压路径，保留路径两端的双引号。
`%base` 和 `%mine` 是 TortoiseSVN 的文件参数，请原样保留。
在 TortoiseSVN 中重新执行 Diff，选择对应工作表即可。

参数和扩展名设置参考 [TortoiseSVN 官方说明](https://tortoisesvn.net/docs/release/TortoiseSVN_en/tsvn-dug-settings.html#tsvn-dug-settings-progs-diff)。

## 更新和回退

更新前记录原来的差异工具命令。可以将此版本解压到独立目录，再将
TortoiseSVN 命令指向该目录；回退时恢复原来的命令即可。

源代码：[lazydao/ExcelMerge](https://github.com/lazydao/ExcelMerge)。
