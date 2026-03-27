#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
AssignSticker 打包脚本

默认使用 Nuitka（standalone）打包，保留 PyInstaller 作为回退方案：
    python build.py
    python build.py --backend nuitka --onefile
    python build.py --backend pyinstaller
"""

import argparse
import os
import shutil
import subprocess
import sys
from datetime import datetime
from pathlib import Path


if sys.platform == "win32":
    import io

    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")
    sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding="utf-8", errors="replace")


APP_NAME = "AssignSticker"
MAIN_SCRIPT = "main.py"

EXCLUDE_DIRS = {
    "__pycache__",
    "build",
    "dist",
    "release",
    "logs",
    "data",
    ".git",
    ".github",
    ".vscode",
    ".idea",
    ".ruff_cache",
    ".venv",
    "venv",
    "env",
    ".codex",
    "scripts",
}
EXCLUDE_FILES = {
    "build.py",
    ".gitignore",
    ".python-version",
}
EXCLUDE_SUFFIXES = {
    ".py",
    ".pyc",
    ".pyo",
    ".spec",
    ".md",
    ".toml",
    ".lock",
}


def get_project_root() -> Path:
    return Path(__file__).parent.absolute()


def clean_build_dirs() -> None:
    print("清理构建目录...")
    project_root = get_project_root()
    for dir_name in ["build", "dist", "__pycache__"]:
        dir_path = project_root / dir_name
        if dir_path.exists():
            shutil.rmtree(dir_path)
            print(f"  已删除: {dir_name}")


def collect_data_entries_nuitka() -> list[str]:
    project_root = get_project_root()
    args: list[str] = []

    print("\n扫描并收集资源（Nuitka）...")
    for item in sorted(project_root.iterdir(), key=lambda p: p.name.lower()):
        name = item.name

        if name in EXCLUDE_FILES or name in EXCLUDE_DIRS:
            print(f"  排除: {name}")
            continue
        if name.startswith("."):
            print(f"  排除隐藏项: {name}")
            continue

        if item.is_dir():
            args.append(f"--include-data-dir={name}={name}")
            print(f"  包含目录: {name}")
            continue

        if item.suffix.lower() in EXCLUDE_SUFFIXES:
            print(f"  排除文件: {name}")
            continue

        args.append(f"--include-data-file={name}={name}")
        print(f"  包含文件: {name}")

    return args


def collect_data_entries_pyinstaller() -> list[str]:
    project_root = get_project_root()
    args: list[str] = []

    print("\n扫描并收集资源（PyInstaller）...")
    for item in sorted(project_root.iterdir(), key=lambda p: p.name.lower()):
        name = item.name

        if name in EXCLUDE_FILES or name in EXCLUDE_DIRS:
            print(f"  排除: {name}")
            continue
        if name.startswith("."):
            print(f"  排除隐藏项: {name}")
            continue

        if item.is_dir():
            args.append(f"--add-data={name};{name}")
            print(f"  包含目录: {name}")
            continue

        if item.suffix.lower() in EXCLUDE_SUFFIXES:
            print(f"  排除文件: {name}")
            continue

        args.append(f"--add-data={name};.")
        print(f"  包含文件: {name}")

    return args


def ensure_backend_installed(backend: str) -> bool:
    try:
        if backend == "nuitka":
            import nuitka  # noqa: F401
            print(f"Nuitka 已安装")
        else:
            import PyInstaller  # noqa: F401
            print(f"PyInstaller 已安装")
        return True
    except ImportError:
        if backend == "nuitka":
            print("错误: 未安装 Nuitka，请先执行: uv pip install nuitka")
        else:
            print("错误: 未安装 PyInstaller，请先执行: uv pip install pyinstaller")
        return False


def run_nuitka(onefile: bool) -> Path:
    data_args = collect_data_entries_nuitka()
    mode_arg = "--onefile" if onefile else "--standalone"

    cmd = [
        sys.executable,
        "-m",
        "nuitka",
        mode_arg,
        "--windows-console-mode=disable",
        f"--output-filename={APP_NAME}",
        "--windows-icon-from-ico=icon.ico",
        "--enable-plugin=tk-inter",
        "--assume-yes-for-downloads",
        "--include-module=webview.platforms.winforms",
        "--include-module=webview.platforms.edgechromium",
        "--include-module=webview.platforms.mshtml",
        "--include-module=pystray._win32",
        "--output-dir=dist",
    ]
    cmd.extend(data_args)
    cmd.append(MAIN_SCRIPT)

    print("\n执行命令:")
    print(" ".join(cmd))
    subprocess.run(cmd, check=True, capture_output=False)

    dist_root = get_project_root() / "dist"
    if onefile:
        return dist_root / f"{APP_NAME}.exe"

    dist_dirs = sorted(dist_root.glob("*.dist"), key=lambda p: p.stat().st_mtime, reverse=True)
    for folder in dist_dirs:
        if (folder / f"{APP_NAME}.exe").exists():
            return folder
    if dist_dirs:
        return dist_dirs[0]
    raise FileNotFoundError("Nuitka 未生成 *.dist 目录")


def run_pyinstaller() -> Path:
    data_args = collect_data_entries_pyinstaller()
    cmd = [
        sys.executable,
        "-m",
        "PyInstaller",
        "--onefile",
        "--windowed",
        f"--name={APP_NAME}",
        "--icon=icon.ico",
        "--clean",
        "--noconfirm",
    ]
    cmd.extend(data_args)
    cmd.append(MAIN_SCRIPT)

    print("\n执行命令:")
    print(" ".join(cmd))
    subprocess.run(cmd, check=True, capture_output=False)
    return get_project_root() / "dist" / f"{APP_NAME}.exe"


def create_release_package(build_output: Path) -> Path:
    print("\n创建发布包...")
    project_root = get_project_root()
    release_dir = project_root / "release" / "AssignSticker-windows"

    if release_dir.exists():
        shutil.rmtree(release_dir)
    release_dir.mkdir(parents=True, exist_ok=True)

    if build_output.is_dir():
        target_dir = release_dir / build_output.name
        shutil.copytree(build_output, target_dir)
        print(f"  复制目录: {build_output.name}")
    elif build_output.exists():
        shutil.copy2(build_output, release_dir / build_output.name)
        print(f"  复制文件: {build_output.name}")
    else:
        raise FileNotFoundError(f"找不到构建产物: {build_output}")

    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    zip_name = f"AssignSticker-windows-x64-{timestamp}"
    zip_path = project_root / "release" / zip_name
    shutil.make_archive(str(zip_path), "zip", root_dir=str(release_dir))
    print(f"  创建压缩包: {zip_name}.zip")
    return zip_path.with_suffix(".zip")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="AssignSticker 打包工具")
    parser.add_argument(
        "--backend",
        choices=["nuitka", "pyinstaller"],
        default="nuitka",
        help="打包后端，默认 nuitka",
    )
    parser.add_argument(
        "--onefile",
        action="store_true",
        help="仅对 nuitka 生效，启用 onefile 模式（默认 standalone）",
    )
    return parser.parse_args()


def main() -> None:
    args = parse_args()

    print("AssignSticker 打包工具")
    print(f"Python: {sys.version}")
    print(f"平台: {sys.platform}")
    print(f"后端: {args.backend}")
    if args.backend == "nuitka":
        print(f"Nuitka 模式: {'onefile' if args.onefile else 'standalone'}")
    print()

    if sys.platform != "win32":
        print("警告: 此脚本主要用于 Windows 构建 EXE")
        print("在 Linux/macOS 上构建的 EXE 无法直接在 Windows 运行")
        response = input("是否继续? (y/N): ")
        if response.lower() != "y":
            print("已取消")
            return

    if not ensure_backend_installed(args.backend):
        sys.exit(1)

    project_root = get_project_root()
    os.chdir(project_root)
    clean_build_dirs()

    try:
        print("\n" + "=" * 60)
        print(f"开始构建 {APP_NAME} Windows 可执行文件")
        print("=" * 60)

        if args.backend == "nuitka":
            build_output = run_nuitka(onefile=args.onefile)
        else:
            build_output = run_pyinstaller()

        zip_file = create_release_package(build_output)

        print("\n" + "=" * 60)
        print("打包完成！")
        print("=" * 60)
        print(f"\n构建产物: {build_output}")
        print(f"发布包: {zip_file}")
    except subprocess.CalledProcessError as e:
        print("\n打包失败，请检查错误信息")
        print(f"错误代码: {e.returncode}")
        sys.exit(1)
    except Exception as e:
        print("\n打包失败")
        print(f"错误: {e}")
        sys.exit(1)


if __name__ == "__main__":
    main()
