import webview
import webview.http as http
import socket
import platform
import os
import sys
import subprocess
import glob
import urllib.parse
import threading
import json
import random
from datetime import datetime
from PIL import Image, ImageDraw
import pystray

# 修改默认端口
http.DEFAULT_HTTP_PORT = 2001

# 日志列表，用于存储所有日志
log_entries = []

# 全局窗口对象
main_window = None

# 调试模式开关
debug_mode = False

# 系统托盘图标对象
tray_icon = None

# 重启标记
should_restart = False

def log(message, level="info"):
    """
    记录日志
    格式: 时间（日期+时间）｜类型（info/error/warning）｜内容
    """
    timestamp = datetime.now().strftime('%Y-%m-%d %H:%M:%S')
    log_entry = f"{timestamp}｜{level}｜{message}"
    log_entries.append(log_entry)
    # 同时输出到控制台
    print(log_entry)

def save_logs():
    """
    保存日志到文件
    文件名格式: 时间_次数.log
    """
    if not log_entries:
        return
    
    # 创建logs目录
    logs_dir = "logs"
    if not os.path.exists(logs_dir):
        os.makedirs(logs_dir)
    
    # 获取当前日期
    date_str = datetime.now().strftime('%Y%m%d')
    
    # 查找当天的日志文件数量
    pattern = os.path.join(logs_dir, f"{date_str}_*.log")
    existing_files = glob.glob(pattern)
    count = len(existing_files) + 1
    
    # 生成文件名
    filename = os.path.join(logs_dir, f"{date_str}_{count}.log")
    
    # 写入日志
    with open(filename, 'w', encoding='utf-8') as f:
        f.write("时间（日期+时间）｜类型（info/error/warning）｜内容\n")
        for entry in log_entries:
            f.write(entry + "\n")
    
    print(f"日志已保存到: {filename}")

def print_system_info():
    """打印系统信息"""
    log("=" * 50, "info")
    log("系统信息", "info")
    log("=" * 50, "info")
    log(f"操作系统: {platform.system()} {platform.release()}", "info")
    log(f"处理器架构: {platform.machine()}", "info")
    log(f"处理器: {platform.processor()}", "info")
    log(f"Python版本: {platform.python_version()}", "info")
    log(f"当前时间: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}", "info")
    log("=" * 50, "info")

def check_single_instance():
    # 创建一个套接字用于检测程序是否已运行
    try:
        # 使用一个固定的端口号作为检测标志
        sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        sock.bind(('127.0.0.1', 9999))
        return True
    except socket.error:
        return False

def create_tray_icon():
    """创建托盘图标"""
    # 尝试加载 icon.png 文件
    icon_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'icon.png')
    if os.path.exists(icon_path):
        try:
            image = Image.open(icon_path)
            # 确保图片是RGBA模式
            if image.mode != 'RGBA':
                image = image.convert('RGBA')
            return image
        except Exception as e:
            log(f"加载 icon.png 失败: {str(e)}，使用默认图标", "warning")
    
    # 如果加载失败，创建默认图标
    width = 64
    height = 64
    image = Image.new('RGBA', (width, height), (0, 0, 0, 0))
    dc = ImageDraw.Draw(image)
    
    # 绘制渐变圆形背景
    for i in range(width):
        for j in range(height):
            # 计算是否在圆内
            if (i - width//2)**2 + (j - height//2)**2 <= (width//2)**2:
                # 渐变效果
                ratio = j / height
                r = int(102 + (118 - 102) * ratio)
                g = int(126 + (75 - 126) * ratio)
                b = int(234 + 162 * ratio)
                dc.point((i, j), fill=(r, g, b, 255))
    
    # 绘制字母 "A"
    dc.text((width//2 - 12, height//2 - 18), "A", fill=(255, 255, 255, 255), font=None)
    
    return image

def show_crash_window(error_msg):
    """显示崩溃窗口"""
    try:
        encoded_error = urllib.parse.quote(error_msg)

        # 创建崩溃窗口
        crash_window = webview.create_window(
            'AssignSticker - 程序崩溃',
            f'htmls/more/crush_screen.html?error={encoded_error}',
            width=500,
            height=400,
            resizable=False
        )

        # 定义API函数供JavaScript调用
        def restart_app():
            """重启应用程序"""
            log("崩溃窗口: 重启程序", "info")
            save_logs()
            # 关闭崩溃窗口
            crash_window.destroy()
            # 重新启动主程序（使用--restart参数跳过多开检测）
            import sys
            import subprocess
            subprocess.Popen([sys.executable, __file__, '--restart'])
            # 退出当前进程
            sys.exit(0)

        def open_url(url):
            """用默认浏览器打开URL"""
            log(f"崩溃窗口: 打开URL {url}", "info")
            import subprocess
            subprocess.call(['open', url])

        def close_window():
            """关闭崩溃窗口并退出程序"""
            log("崩溃窗口: 关闭窗口", "info")
            save_logs()
            crash_window.destroy()
            sys.exit(0)

        # 暴露API给JavaScript
        crash_window.expose(restart_app)
        crash_window.expose(open_url)
        crash_window.expose(close_window)

        webview.start()
    except Exception as e:
        log(f"显示崩溃窗口失败: {str(e)}", "error")

def setup_tray_icon(window):
    """设置系统托盘图标"""
    global tray_icon

    def on_show_window(icon, item):
        """显示主窗口"""
        log("托盘菜单: 显示主窗口", "info")
        if window:
            window.show()
            window.restore()

    def on_toggle_devtools(icon, item):
        """切换开发人员工具"""
        log("托盘菜单: 切换开发人员工具", "info")
        # 保存日志
        save_logs()
        # 停止托盘图标
        icon.stop()
        # 使用子进程重新启动程序，启用调试模式
        subprocess.Popen([sys.executable, __file__, '--with-devtools'])
        # 退出当前程序
        if window:
            window.destroy()
        sys.exit(0)

    def on_trigger_crash(icon, item):
        """触发异常（测试崩溃窗口）"""
        log("托盘菜单: 触发异常测试", "warning")
        # 保存日志
        save_logs()
        # 停止托盘图标
        icon.stop()
        # 使用子进程显示崩溃窗口，然后退出主程序
        import subprocess
        error_msg = "这是从托盘菜单手动触发的测试异常，用于测试崩溃窗口功能"
        encoded_error = urllib.parse.quote(error_msg)
        subprocess.Popen([sys.executable, __file__, '--crash-window', encoded_error])
        # 退出主程序
        if window:
            window.destroy()
        sys.exit(0)

    def on_open_logs(icon, item):
        """打开日志文件夹"""
        log("托盘菜单: 打开日志文件夹", "info")
        import subprocess
        logs_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'logs')
        if os.path.exists(logs_path):
            subprocess.call(['open', logs_path])
            log("已打开日志文件夹", "info")
        else:
            log("日志文件夹不存在，正在创建...", "warning")
            os.makedirs(logs_path)
            subprocess.call(['open', logs_path])

    def on_exit(icon, item):
        """退出程序"""
        log("托盘菜单: 退出程序", "info")
        save_logs()
        icon.stop()
        if window:
            window.destroy()

    # 创建托盘菜单
    menu = pystray.Menu(
        pystray.MenuItem("显示主窗口", on_show_window),
        pystray.Menu.SEPARATOR,
        pystray.MenuItem("调试", pystray.Menu(
            pystray.MenuItem("开发人员工具", on_toggle_devtools),
            pystray.MenuItem("触发异常（测试）", on_trigger_crash),
        )),
        pystray.MenuItem("打开日志文件夹", on_open_logs),
        pystray.Menu.SEPARATOR,
        pystray.MenuItem("退出", on_exit)
    )

    # 创建托盘图标
    icon = pystray.Icon(
        "AssignSticker",
        create_tray_icon(),
        "AssignSticker",
        menu
    )

    tray_icon = icon

    # 在macOS上，托盘图标需要在主线程运行
    # 使用run_detached方法在后台运行
    icon.run_detached()

    log("系统托盘图标已启动", "info")

def show_crash_window_standalone(encoded_error):
    """独立显示崩溃窗口（用于子进程模式）"""
    try:
        crash_window = webview.create_window(
            'AssignSticker - 程序崩溃',
            f'htmls/more/crush_screen.html?error={encoded_error}',
            width=500,
            height=400,
            resizable=False
        )

        def restart_app():
            """重启应用程序"""
            crash_window.destroy()
            subprocess.Popen([sys.executable, __file__, '--restart'])
            sys.exit(0)

        def open_url(url):
            """用默认浏览器打开URL"""
            subprocess.call(['open', url])

        def close_window():
            """关闭崩溃窗口并退出程序"""
            crash_window.destroy()
            sys.exit(0)

        crash_window.expose(restart_app)
        crash_window.expose(open_url)
        crash_window.expose(close_window)

        webview.start()
    except Exception as e:
        print(f"显示崩溃窗口失败: {str(e)}")

def ensure_data_directory():
    """确保data目录和homework_save、homework_save_auto目录存在，不存在则创建"""
    data_dir = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'data')
    if not os.path.exists(data_dir):
        os.makedirs(data_dir)
        log(f"创建data目录: {data_dir}", "info")
    
    homework_save_dir = os.path.join(data_dir, 'homework_save')
    if not os.path.exists(homework_save_dir):
        os.makedirs(homework_save_dir)
        log(f"创建homework_save目录: {homework_save_dir}", "info")
    
    homework_save_auto_dir = os.path.join(data_dir, 'homework_save_auto')
    if not os.path.exists(homework_save_auto_dir):
        os.makedirs(homework_save_auto_dir)
        log(f"创建homework_save_auto目录: {homework_save_auto_dir}", "info")
    
    return data_dir


def get_homework_save_dir():
    """获取作业保存目录路径"""
    return os.path.join(os.path.dirname(os.path.abspath(__file__)), 'data', 'homework_save')


def get_homework_save_auto_dir():
    """获取自动保存作业目录路径"""
    return os.path.join(os.path.dirname(os.path.abspath(__file__)), 'data', 'homework_save_auto')


class Api:
    """暴露给前端调用的API"""
    
    def __init__(self):
        self.window = None
        self.should_restart = False
    
    def saveHomeworkToFile(self, homework_data):
        """
        保存作业数据到JSON文件
        homework_data: 作业数据对象或数组
        """
        try:
            save_dir = get_homework_save_dir()
            
            # 生成文件名：时间戳_随机数.json
            timestamp = datetime.now().strftime('%Y%m%d_%H%M%S')
            import random
            random_num = random.randint(1000, 9999)
            filename = f"homework_{timestamp}_{random_num}.json"
            filepath = os.path.join(save_dir, filename)
            
            # 写入JSON文件
            with open(filepath, 'w', encoding='utf-8') as f:
                json.dump(homework_data, f, ensure_ascii=False, indent=2)
            
            log(f"作业已保存到: {filepath}", "info")
            return {"success": True, "message": f"作业已保存到: {filename}", "filepath": filepath}
        except Exception as e:
            error_msg = str(e)
            log(f"保存作业失败: {error_msg}", "error")
            return {"success": False, "message": f"保存失败: {error_msg}"}
    
    def getSavedHomeworkFiles(self):
        """获取已保存的作业文件列表"""
        try:
            save_dir = get_homework_save_dir()
            if not os.path.exists(save_dir):
                return {"success": True, "files": []}
            
            files = []
            for filename in os.listdir(save_dir):
                if filename.endswith('.json'):
                    filepath = os.path.join(save_dir, filename)
                    stat = os.stat(filepath)
                    files.append({
                        "filename": filename,
                        "created": datetime.fromtimestamp(stat.st_ctime).strftime('%Y-%m-%d %H:%M:%S'),
                        "size": stat.st_size
                    })
            
            # 按创建时间排序
            files.sort(key=lambda x: x["created"], reverse=True)
            return {"success": True, "files": files}
        except Exception as e:
            error_msg = str(e)
            log(f"获取作业文件列表失败: {error_msg}", "error")
            return {"success": False, "message": f"获取失败: {error_msg}"}
    
    def loadHomeworkFromFile(self, filename):
        """从文件加载作业数据"""
        try:
            save_dir = get_homework_save_dir()
            filepath = os.path.join(save_dir, filename)
            
            # 安全检查：确保文件在保存目录内
            if not filepath.startswith(save_dir):
                return {"success": False, "message": "非法文件路径"}
            
            if not os.path.exists(filepath):
                return {"success": False, "message": "文件不存在"}
            
            with open(filepath, 'r', encoding='utf-8') as f:
                data = json.load(f)
            
            return {"success": True, "data": data}
        except Exception as e:
            error_msg = str(e)
            log(f"加载作业文件失败: {error_msg}", "error")
            return {"success": False, "message": f"加载失败: {error_msg}"}
    
    def autoSaveHomework(self, homework_data):
        """
        自动保存作业数据到JSON文件
        homework_data: 作业数据对象或数组
        """
        try:
            save_dir = get_homework_save_auto_dir()
            
            # 生成文件名：auto_时间戳.json
            timestamp = datetime.now().strftime('%Y%m%d_%H%M%S')
            filename = f"auto_{timestamp}.json"
            filepath = os.path.join(save_dir, filename)
            
            # 写入JSON文件
            with open(filepath, 'w', encoding='utf-8') as f:
                json.dump(homework_data, f, ensure_ascii=False, indent=2)
            
            log(f"作业已自动保存到: {filepath}", "info")
            return {"success": True, "message": f"自动保存成功: {filename}", "filepath": filepath}
        except Exception as e:
            error_msg = str(e)
            log(f"自动保存作业失败: {error_msg}", "error")
            return {"success": False, "message": f"自动保存失败: {error_msg}"}
    
    def restartApp(self):
        """重启应用程序"""
        try:
            log("用户触发重启", "info")
            global should_restart
            # 先标记需要重启
            should_restart = True
            # 保存日志
            save_logs()
            # 停止托盘图标
            global tray_icon
            if tray_icon:
                tray_icon.stop()
            # 关闭当前窗口并强制退出进程
            if self.window:
                self.window.destroy()
            # 强制退出当前进程
            os._exit(0)
        except Exception as e:
            error_msg = str(e)
            log(f"重启失败: {error_msg}", "error")
            return {"success": False, "message": f"重启失败: {error_msg}"}
    
    def exitApp(self):
        """退出应用程序"""
        try:
            log("用户触发退出", "info")
            # 保存日志
            save_logs()
            # 停止托盘图标
            global tray_icon
            if tray_icon:
                tray_icon.stop()
            # 关闭窗口并退出进程
            if self.window:
                self.window.destroy()
            # 强制退出进程
            os._exit(0)
        except Exception as e:
            error_msg = str(e)
            log(f"退出失败: {error_msg}", "error")
            return {"success": False, "message": f"退出失败: {error_msg}"}

if __name__ == '__main__':
    # 检查是否是崩溃窗口模式
    if '--crash-window' in sys.argv:
        # 获取编码后的错误信息
        crash_index = sys.argv.index('--crash-window')
        if crash_index + 1 < len(sys.argv):
            encoded_error = sys.argv[crash_index + 1]
            show_crash_window_standalone(encoded_error)
        sys.exit(0)

    # 检查是否启用开发者工具模式
    show_devtools = '--with-devtools' in sys.argv

    try:
        # 确保data目录存在
        ensure_data_directory()

        # 打印系统信息
        print_system_info()

        # 检查是否是重启模式
        is_restart = '--restart' in sys.argv

        # 检查程序是否已运行（重启模式和开发者工具模式跳过检测）
        if not is_restart and not show_devtools and not check_single_instance():
            log("检测到程序已在运行中", "warning")
            # 程序已运行，打开提示窗口
            webview.create_window('程序已运行', 'doubletips.html', width=400, height=300, resizable=False)
            webview.start()
        else:
            if is_restart:
                log("重启模式：跳过多开检测", "info")
            if show_devtools:
                log("开发者工具模式：跳过多开检测", "info")
            log("程序启动成功", "info")
            # 创建API实例
            api = Api()
            
            # 创建无边框窗口
            main_window = webview.create_window(
                'Wow 伙伴！',
                'index.html',
                frameless=True,
                width=2296,
                height=1136,
                resizable=False,
                on_top=False,
                js_api=api
            )
            
            # 将窗口对象保存到API中，以便API方法可以访问
            api.window = main_window

            # 在主线程设置托盘图标（必须在start之前）
            setup_tray_icon(main_window)

            # 注册拖拽区域
            def on_loaded():
                log("注册拖拽区域", "info")
                # 仅允许在html标签内拖拽
                main_window.evaluate_js("""
                    document.querySelector('html').addEventListener('mousedown', function(e) {
                        window.dragStart();
                    });
                """)

            # 启动程序（根据参数决定是否启用开发者工具）
            webview.start(
                on_loaded,
                private_mode=True,
                http_server=True,
                debug=show_devtools
            )
    except Exception as e:
        error_msg = str(e)
        log(f"程序异常: {error_msg}", "error")

        # 显示崩溃窗口
        show_crash_window(error_msg)
    finally:
        # 检查是否需要重启（重启时已在restartApp中处理）
        if should_restart:
            log("正在重启应用程序...", "info")
            subprocess.Popen([sys.executable, __file__])
            sys.exit(0)
        
        # 程序退出时保存日志
        save_logs()
        # 停止托盘图标
        if tray_icon:
            tray_icon.stop()
