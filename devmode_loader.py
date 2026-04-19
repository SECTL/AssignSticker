import os


def ensure_debug_mode():
    """强制在 data 目录下创建 .debug_mode 文件"""
    data_dir = os.path.join(
        os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "data"
    )
    debug_file = os.path.join(data_dir, ".debug_mode")
    os.makedirs(data_dir, exist_ok=True)
    with open(debug_file, "w") as f:
        f.write("1")
