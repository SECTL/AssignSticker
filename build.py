#!/usr/bin/env python3
"""
AssignSticker 打包脚本
使用 PyInstaller 打包成 Windows x64 可执行文件
"""

import os
import sys
import shutil
import subprocess
from pathlib import Path
from datetime import datetime


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
    """获取所有需要打包的数据文件和目录"""
    project_root = get_project_root()

    # 定义需要包含的目录
    include_dirs = [
        'icons',
        'htmls',
        'saying',
        'desktop_widgets',
    ]

    # 定义需要包含的单个文件
    include_files = [
        'font.ttf',
        'icon.ico',
        'introduce',
        'banner.png',
        'LICENSE',
        'README.md',
    ]

    datas = []

    # 添加目录
    for dir_name in include_dirs:
        dir_path = project_root / dir_name
        if dir_path.exists():
            # PyInstaller --add-data 格式: "源路径;目标路径"
            datas.append(f"--add-data={dir_name};{dir_name}")
            print(f"  包含目录: {dir_name}")
        else:
            print(f"  警告: 目录不存在 {dir_name}")

    # 添加单个文件
    for file_name in include_files:
        file_path = project_root / file_name
        if file_path.exists():
            datas.append(f"--add-data={file_name};.")
            print(f"  包含文件: {file_name}")
        else:
            print(f"  警告: 文件不存在 {file_name}")

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
    """创建发布包"""
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

    # 复制其他文件和目录
    copy_items = [
        'icons',
        'htmls',
        'saying',
        'desktop_widgets',
        'font.ttf',
        'icon.ico',
        'introduce',
        'banner.png',
        'LICENSE',
        'README.md',
    ]

    for item in copy_items:
        source = project_root / item
        target = release_dir / item

        if not source.exists():
            print(f"  跳过: {item} (不存在)")
            continue

        try:
            if source.is_dir():
                shutil.copytree(source, target, ignore=shutil.ignore_patterns('__pycache__', '*.pyc'))
                print(f"  复制目录: {item}")
            else:
                shutil.copy2(source, target)
                print(f"  复制文件: {item}")
        except Exception as e:
            print(f"  错误复制 {item}: {e}")

    # 创建 ZIP 压缩包
    print("\n创建 ZIP 压缩包...")
    timestamp = datetime.now().strftime('%Y%m%d_%H%M%S')
    zip_name = f"AssignSticker-windows-x64-{timestamp}"
    zip_path = project_root / 'release' / zip_name

    try:
        shutil.make_archive(str(zip_path), 'zip', str(release_dir))
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
    else:
        print("\n打包失败，请检查错误信息")
        sys.exit(1)


if __name__ == '__main__':
    main()
