#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
AssignSticker 打包脚本

默认使用 Nuitka（standalone）打包，支持 Windows/Linux：
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


def get_platform_key() -> str:
    if sys.platform == "win32":
        return "windows"
    if sys.platform.startswith("linux"):
        return "linux"
    if sys.platform == "darwin":
        return "macos"
    return sys.platform


def get_arch_label() -> str:
    machine = os.uname().machine.lower() if hasattr(os, "uname") else ""
    if not machine and sys.platform == "win32":
        machine = os.environ.get("PROCESSOR_ARCHITECTURE", "").lower()

    aliases = {
        "amd64": "x64",
        "x86_64": "x64",
        "x64": "x64",
        "arm64": "arm64",
        "aarch64": "arm64",
    }
    return aliases.get(machine, machine or "unknown")


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
    separator = ";" if sys.platform == "win32" else ":"

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
            args.append(f"--add-data={name}{separator}{name}")
            print(f"  包含目录: {name}")
            continue

        if item.suffix.lower() in EXCLUDE_SUFFIXES:
            print(f"  排除文件: {name}")
            continue

        args.append(f"--add-data={name}{separator}.")
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
    platform_key = get_platform_key()

    cmd = [
        sys.executable,
        "-m",
        "nuitka",
        mode_arg,
        f"--output-filename={APP_NAME}",
        "--enable-plugin=tk-inter",
        "--assume-yes-for-downloads",
        "--output-dir=dist",
    ]

    if platform_key == "windows":
        cmd.extend(
            [
                "--windows-console-mode=disable",
                "--windows-icon-from-ico=icon.ico",
                "--include-module=webview.platforms.winforms",
                "--include-module=webview.platforms.edgechromium",
                "--include-module=webview.platforms.mshtml",
                "--include-module=pystray._win32",
            ]
        )
    elif platform_key == "linux":
        cmd.extend(
            [
                "--include-module=webview.platforms.gtk",
                "--include-module=pystray._appindicator",
                "--include-module=pystray._gtk",
            ]
        )
    else:
        print(f"提示: 当前平台 {platform_key} 未设置专用 Nuitka 模块参数，使用通用配置")

    cmd.extend(data_args)
    cmd.append(MAIN_SCRIPT)

    print("\n执行命令:")
    print(" ".join(cmd))
    subprocess.run(cmd, check=True, capture_output=False)

    dist_root = get_project_root() / "dist"
    if onefile:
        onefile_candidates = [
            dist_root / f"{APP_NAME}.exe",
            dist_root / APP_NAME,
            dist_root / f"{APP_NAME}.bin",
        ]
        for candidate in onefile_candidates:
            if candidate.exists():
                return candidate
        recent_files = sorted(
            [p for p in dist_root.iterdir() if p.is_file()],
            key=lambda p: p.stat().st_mtime,
            reverse=True,
        )
        if recent_files:
            return recent_files[0]
        raise FileNotFoundError("Nuitka 未生成 onefile 产物")

    dist_dirs = sorted(
        dist_root.glob("*.dist"), key=lambda p: p.stat().st_mtime, reverse=True
    )
    for folder in dist_dirs:
        if any(
            (folder / name).exists()
            for name in (f"{APP_NAME}.exe", APP_NAME, f"{APP_NAME}.bin")
        ):
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
    dist_root = get_project_root() / "dist"
    candidates = [
        dist_root / f"{APP_NAME}.exe",
        dist_root / APP_NAME,
    ]
    for candidate in candidates:
        if candidate.exists():
            return candidate
    raise FileNotFoundError("PyInstaller 未生成预期可执行文件")


def create_release_package(build_output: Path) -> Path:
    print("\n创建发布包...")
    project_root = get_project_root()
    platform_key = get_platform_key()
    arch = get_arch_label()
    release_dir = project_root / "release" / f"{APP_NAME}-{platform_key}"

    if release_dir.exists():
        shutil.rmtree(release_dir)
    release_dir.mkdir(parents=True, exist_ok=True)

    if build_output.is_dir():
        copied_count = 0
        for item in build_output.iterdir():
            target = release_dir / item.name
            if item.is_dir():
                shutil.copytree(item, target)
            else:
                shutil.copy2(item, target)
            copied_count += 1
        print(f"  已平铺复制目录内容: {build_output.name} ({copied_count} 项)")
    elif build_output.exists():
        shutil.copy2(build_output, release_dir / build_output.name)
        print(f"  复制文件: {build_output.name}")
    else:
        raise FileNotFoundError(f"找不到构建产物: {build_output}")

    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    zip_name = f"{APP_NAME}-{platform_key}-{arch}-{timestamp}"
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
    platform_key = get_platform_key()

    print("AssignSticker 打包工具")
    print(f"Python: {sys.version}")
    print(f"平台: {sys.platform}")
    print(f"后端: {args.backend}")
    if args.backend == "nuitka":
        print(f"Nuitka 模式: {'onefile' if args.onefile else 'standalone'}")
    print()

    if platform_key not in {"windows", "linux"}:
        print(f"警告: 当前平台 {platform_key} 未正式支持，可能打包失败")
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
        print(f"开始构建 {APP_NAME} {platform_key} 可执行文件")
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
