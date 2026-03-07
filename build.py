#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
AssignSticker 打包脚本
使用 PyInstaller 打包成 Windows x64 可执行文件
打包项目根目录下的所有内容（排除特定文件和目录）
"""

import os
import sys
import shutil
import subprocess
from pathlib import Path
from datetime import datetime

# 设置 Windows 控制台编码为 UTF-8
if sys.platform == 'win32':
    import io
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
    sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8', errors='replace')


def get_project_root():
    """获取项目根目录"""
    return Path(__file__).parent.absolute()


def clean_build_dirs():
    """清理构建目录"""
    print("清理构建目录...")
    dirs_to_clean = ['build', 'dist', '__pycache__']
    for dir_name in dirs_to_clean:
        dir_path = get_project_root() / dir_name
        if dir_path.exists():
            shutil.rmtree(dir_path)
            print(f"  已删除: {dir_name}")


def get_all_data_files():
    """获取所有需要打包的数据文件和目录（自动扫描根目录）"""
    project_root = get_project_root()

    # 定义需要排除的文件和目录
    exclude_items = {
        # Python 相关
        'build.py',           # 本打包脚本
        'build',              # 构建目录
        'dist',               # 输出目录
        '__pycache__',        # Python 缓存
        '*.pyc',              # 编译后的 Python 文件
        '*.pyo',              # 优化的 Python 文件
        '.git',               # Git 目录
        '.gitignore',         # Git 忽略文件
        '.github',            # GitHub 工作流目录
        'release',            # 发布目录
        'logs',               # 日志目录
        'data',               # 运行时数据目录
        # IDE 相关
        '.vscode',            # VS Code 配置
        '.idea',              # PyCharm 配置
        '*.spec',             # PyInstaller spec 文件
    }

    datas = []

    print("\n扫描项目根目录...")

    # 遍历项目根目录下的所有项目
    for item in project_root.iterdir():
        item_name = item.name

        # 检查是否在排除列表中
        if item_name in exclude_items:
            print(f"  排除: {item_name}")
            continue

        # 跳过隐藏文件（以.开头）
        if item_name.startswith('.') and item_name != '.':
            print(f"  排除隐藏项: {item_name}")
            continue

        # 跳过 Python 缓存文件
        if item_name.endswith(('.pyc', '.pyo', '.spec')):
            print(f"  排除缓存: {item_name}")
            continue

        if item.is_dir():
            # 目录: --add-data=目录名;目录名
            datas.append(f"--add-data={item_name};{item_name}")
            print(f"  包含目录: {item_name}")
        elif item.is_file():
            # 文件: --add-data=文件名;.
            datas.append(f"--add-data={item_name};.")
            print(f"  包含文件: {item_name}")

    return datas


def build_exe():
    """构建可执行文件"""
    print("\n" + "="*60)
    print("开始构建 AssignSticker Windows 可执行文件")
    print("="*60 + "\n")

    project_root = get_project_root()
    os.chdir(project_root)

    # 清理旧的构建文件
    clean_build_dirs()

    # 获取数据文件参数
    print("\n收集数据文件...")
    data_args = get_all_data_files()

    # 构建 PyInstaller 命令
    cmd = [
        sys.executable, '-m', 'PyInstaller',
        '--onefile',           # 单文件模式
        '--windowed',          # 窗口模式（无控制台）
        '--name=AssignSticker', # 输出文件名
        '--icon=icon.ico',     # 图标文件
        '--clean',             # 清理临时文件
        '--noconfirm',         # 不确认覆盖
    ]

    # 添加数据文件参数
    cmd.extend(data_args)

    # 添加主程序文件
    cmd.append('main.py')

    print("\n执行命令:")
    print(' '.join(cmd))
    print()

    # 执行构建
    try:
        result = subprocess.run(cmd, check=True, capture_output=False)
        print("\n" + "="*60)
        print("构建成功！")
        print("="*60)
        return True
    except subprocess.CalledProcessError as e:
        print("\n" + "="*60)
        print("构建失败！")
        print("="*60)
        print(f"错误代码: {e.returncode}")
        return False


def create_distribution():
    """创建发布包（复制dist目录下的exe文件）"""
    print("\n创建发布包...")

    project_root = get_project_root()
    dist_dir = project_root / 'dist'
    release_dir = project_root / 'release' / 'AssignSticker-windows'

    # 创建发布目录
    if release_dir.exists():
        shutil.rmtree(release_dir)
    release_dir.mkdir(parents=True, exist_ok=True)

    # 复制可执行文件
    exe_source = dist_dir / 'AssignSticker.exe'
    exe_target = release_dir / 'AssignSticker.exe'

    if exe_source.exists():
        shutil.copy2(exe_source, exe_target)
        print(f"  复制: {exe_source.name}")
    else:
        print(f"  错误: 找不到 {exe_source}")
        return False

    # 创建 ZIP 压缩包
    print("\n创建 ZIP 压缩包...")
    timestamp = datetime.now().strftime('%Y%m%d_%H%M%S')
    zip_name = f"AssignSticker-windows-x64-{timestamp}"
    zip_path = project_root / 'release' / zip_name

    try:
        shutil.make_archive(str(zip_path), 'zip', str(dist_dir))
        print(f"  创建: {zip_name}.zip")
    except Exception as e:
        print(f"  错误创建 ZIP: {e}")
        return False

    print(f"\n发布包位置: {zip_path}.zip")
    return True


def main():
    """主函数"""
    print("AssignSticker 打包工具")
    print(f"Python: {sys.version}")
    print(f"平台: {sys.platform}")
    print()

    if sys.platform != 'win32':
        print("警告: 此脚本设计用于 Windows 平台")
        print("在 Linux/macOS 上构建的 EXE 将无法在 Windows 上运行")
        response = input("是否继续? (y/N): ")
        if response.lower() != 'y':
            print("已取消")
            return

    # 检查 PyInstaller
    try:
        import PyInstaller
        print(f"PyInstaller 版本: {PyInstaller.__version__}")
    except ImportError:
        print("错误: 未安装 PyInstaller")
        print("请运行: pip install pyinstaller")
        return

    # 构建
    if build_exe():
        # 创建发布包
        create_distribution()

        print("\n" + "="*60)
        print("打包完成！")
        print("="*60)
        print(f"\n可执行文件: dist/AssignSticker.exe")
        print(f"发布包: release/AssignSticker-windows-x64-*.zip")
        print("\n注意: 所有资源文件已通过 --add-data 打包到exe内部")
    else:
        print("\n打包失败，请检查错误信息")
        sys.exit(1)


if __name__ == '__main__':
    main()
