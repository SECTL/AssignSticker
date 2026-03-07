# AssignSticker

<div align="center">

<image src="./icon.png"/>

<image src="./banner.png"/>

[![GitHub Issues](https://img.shields.io/github/issues-search/SECTL/AssignSticker?query=is%3Aopen&style=for-the-badge&color=00b4ab&logo=github&label=问题)](https://github.com/SECTL/AssignSticker/issues)
[![最新版本](https://img.shields.io/github/v/release/SECTL/AssignSticker?style=for-the-badge&color=00b4ab&label=最新正式版)](https://github.com/SECTL/AssignSticker/releases/latest)
[![最新Beta版本](https://img.shields.io/github/v/release/SECTL/AssignSticker?include_prereleases&style=for-the-badge&label=测试版)](https://github.com/SECTL/AssignSticker/releases/)
[![上次更新](https://img.shields.io/github/last-commit/SECTL/AssignSticker?style=for-the-badge&color=00b4ab&label=最后更新时间)](https://github.com/SECTL/AssignSticker/commits/master)
[![下载统计](https://img.shields.io/github/downloads/SECTL/AssignSticker/total?style=for-the-badge&color=00b4ab&label=累计下载)](https://github.com/SECTL/AssignSticker/releases)

[![QQ群](https://img.shields.io/badge/-QQ%E7%BE%A4%EF%BD%9C833875216-blue?style=for-the-badge&logo=QQ)](https://qm.qq.com/q/iWcfaPHn7W)

</div>

一个使用Pywebview开发的作业看板项目
## 主要功能

- 作业板主窗口：无边框可拖动，顶部显示时间日期，主体以卡片展示各学科的作业
- 布置新作业：弹窗表单支持选择学科、标题、练习册页码、备注与标签
- 收起与迷你悬浮球：主窗收起后，右侧出现 88×88 的悬浮球，一键唤起
- 系统托盘菜单：显示/隐藏小组件、打开设置、重启、退出
- 设置窗口（侧边栏分页）：通用、学科管理、安全、个性化、其他、关于
- 初次引导：首次使用或重置后进入，引导完成后进入主作业板
- 开发角标：通过 `dev.txt` 开关显示“开发中版本”提示

## 运行环境

- Windows 10/11
- Python 3.10+
- 依赖：PyQt5

## CI 构建与发布

- 工作流文件：`.github/workflows/build.yaml`（仅 Windows 构建）  
  链接：[build.yaml](file:///d:/GitHub/AssignSticker/.github/workflows/build.yaml)
- 手动触发：`workflow_dispatch`，需输入 `version`（版本 tag），支持区分 beta 与正式
- 构建产物命名规则：`[软件]-[版本tag]-[架构]-[系统].zip`
  - 示例（正式）：`AssignSticker-1.2.0.2-x64-windows.zip`
  - 示例（beta）：`AssignSticker-1.2.0.2-beta1-x64-windows.zip`
- 发布策略：当 `version` 包含 `-beta` 时，Release 自动标记为预发布（pre-release）

## 版权与说明

- Copyright © 2025-2026 SECTL

