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

# 根据操作系统设置默认的GUI后端
system = platform.system()
if system == 'Linux':
    # Linux系统默认使用GTK后端
    os.environ['WEBVIEW_GUI'] = 'gtk'

# 修改默认端口
http.DEFAULT_HTTP_PORT = 2001

# 日志列表，用于存储所有日志
log_entries = []

# 全局窗口对象
main_window = None
widget_window = None
is_main_window_hidden = False

# 调试模式开关
debug_mode = False

# 系统托盘图标对象
tray_icon = None

# 重启标记
should_restart = False

def log(message, level="info"):
    """
    记录日志
    格式: 时间（日期+时间）| 类型（info/error/warning）| 内容
    """
    timestamp = datetime.now().strftime('%Y-%m-%d %H:%M:%S')
    log_entry = f"{timestamp} | {level} | {message}"
    log_entries.append(log_entry)
    # 同时输出到控制台
    try:
        print(log_entry)
    except UnicodeEncodeError:
        # Windows 控制台编码问题，使用 ASCII 字符
        print(f"{timestamp} | {level} | {message.encode('ascii', 'replace').decode('ascii')}")

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
        f.write("时间（日期+时间）| 类型（info/error/warning）| 内容\n")
        for entry in log_entries:
            f.write(entry + "\n")
    
    try:
        print(f"日志已保存到: {filename}")
    except UnicodeEncodeError:
        print(f"Logs saved to: {filename}")

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
    
    # 确保 settings.json 存在
    settings_file = os.path.join(data_dir, 'settings.json')
    if not os.path.exists(settings_file):
        default_settings = {
            "theme": "blue",
            "fontSize": 14,
            "opacity": 100,
            "glassEffect": False,
            "showSaying": True,
            "showSeconds": True,
            "toolbarPosition": "center",
            "enableAnimation": True,
            "animationSpeed": "normal",
            "autoStart": False,
            "startMinimized": False,
            "enableReminder": True,
            "reminderTime": "30分钟",
            "autoSaveInterval": "5分钟"
        }
        with open(settings_file, 'w', encoding='utf-8') as f:
            json.dump(default_settings, f, ensure_ascii=False, indent=2)
        log(f"创建默认设置文件: {settings_file}", "info")
    
    return data_dir


def get_homework_save_dir():
    """获取作业保存目录路径"""
    return os.path.join(os.path.dirname(os.path.abspath(__file__)), 'data', 'homework_save')


def get_homework_save_auto_dir():
    """获取自动保存作业目录路径"""
    return os.path.join(os.path.dirname(os.path.abspath(__file__)), 'data', 'homework_save_auto')


class WidgetApi:
    """小组件窗口的API"""
    
    def show_main_window(self):
        """显示主窗口"""
        global is_main_window_hidden, widget_window, main_window
        log("小组件: 显示主窗口", "info")
        is_main_window_hidden = False
        
        if main_window:
            main_window.show()
            main_window.restore()
        
        # 隐藏小组件
        if widget_window:
            widget_window.hide()
    
    def move_widget(self, delta_x, delta_y):
        """移动小组件窗口"""
        global widget_window
        if widget_window:
            try:
                x, y = widget_window.x, widget_window.y
                widget_window.move(x + delta_x, y + delta_y)
            except Exception as e:
                log(f"移动小组件失败: {str(e)}", "error")


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

    def hideMainWindow(self):
        """隐藏主窗口并显示小组件"""
        try:
            global is_main_window_hidden, widget_window
            log("隐藏主窗口", "info")
            is_main_window_hidden = True
            
            # 隐藏主窗口
            if self.window:
                self.window.hide()
            
            # 创建小组件窗口
            if widget_window is None:
                widget_window = webview.create_window(
                    'AssignSticker Widget',
                    'desktop_widgets/desktop_widgets.html',
                    width=240,
                    height=60,
                    frameless=True,
                    on_top=True,
                    resizable=False,
                    transparent=True,
                    js_api=WidgetApi()
                )
            else:
                widget_window.show()
            
            return {"success": True, "message": "主窗口已隐藏"}
        except Exception as e:
            error_msg = str(e)
            log(f"隐藏主窗口失败: {error_msg}", "error")
            return {"success": False, "message": f"隐藏失败: {error_msg}"}

    def showMainWindow(self):
        """显示主窗口并隐藏小组件"""
        try:
            global is_main_window_hidden, widget_window
            log("显示主窗口", "info")
            is_main_window_hidden = False
            
            # 显示主窗口
            if self.window:
                self.window.show()
                self.window.restore()
            
            # 隐藏小组件
            if widget_window:
                widget_window.hide()
            
            return {"success": True, "message": "主窗口已显示"}
        except Exception as e:
            error_msg = str(e)
            log(f"显示主窗口失败: {error_msg}", "error")
            return {"success": False, "message": f"显示失败: {error_msg}"}

    def openSettingsWindow(self):
        """打开设置窗口"""
        try:
            log("打开设置窗口", "info")
            # 创建设置窗口
            settings_window = webview.create_window(
                '设置 - AssignSticker',
                'htmls/settingspage/settingswindow.html',
                width=800,
                height=600,
                resizable=True,
                min_size=(600, 400),
                background_color='#f5f5f5'
            )
            return {"success": True, "message": "设置窗口已打开"}
        except Exception as e:
            error_msg = str(e)
            log(f"打开设置窗口失败: {error_msg}", "error")
            return {"success": False, "message": f"打开设置窗口失败: {error_msg}"}

    def setZoom(self, zoom):
        """设置主窗口缩放比例"""
        try:
            if self.window:
                self.window.evaluate_js(f"document.body.style.zoom = '{zoom}%'")
                log(f"设置缩放比例为 {zoom}%", "info")
                return {"success": True, "message": f"缩放比例已设置为 {zoom}%"}
            else:
                return {"success": False, "message": "主窗口不存在"}
        except Exception as e:
            error_msg = str(e)
            log(f"设置缩放比例失败: {error_msg}", "error")
            return {"success": False, "message": f"设置失败: {error_msg}"}

    def saveSettings(self, settings):
        """保存设置到文件"""
        try:
            settings_file = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'data', 'settings.json')
            with open(settings_file, 'w', encoding='utf-8') as f:
                json.dump(settings, f, ensure_ascii=False, indent=2)
            log(f"设置已保存", "info")
            
            # 如果有缩放设置，应用到主窗口
            if 'zoom' in settings and self.window:
                self.window.evaluate_js(f"document.body.style.zoom = '{settings['zoom']}%'")
            
            return {"success": True, "message": "设置已保存"}
        except Exception as e:
            error_msg = str(e)
            log(f"保存设置失败: {error_msg}", "error")
            return {"success": False, "message": f"保存设置失败: {error_msg}"}

    def loadSettings(self):
        """从文件加载设置"""
        try:
            settings_file = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'data', 'settings.json')
            if os.path.exists(settings_file):
                with open(settings_file, 'r', encoding='utf-8') as f:
                    settings = json.load(f)
                return {"success": True, "data": settings}
            else:
                # 返回默认设置
                default_settings = {
                    "theme": "blue",
                    "fontSize": 14,
                    "zoom": 100,
                    "opacity": 100,
                    "glassEffect": False,
                    "showSaying": True,
                    "showSeconds": True,
                    "toolbarPosition": "center",
                    "enableAnimation": True,
                    "animationSpeed": "normal",
                    "autoStart": False,
                    "startMinimized": False,
                    "enableReminder": True,
                    "reminderTime": "30分钟",
                    "autoSaveInterval": "5分钟"
                }
                return {"success": True, "data": default_settings}
        except Exception as e:
            error_msg = str(e)
            log(f"加载设置失败: {error_msg}", "error")
            return {"success": False, "message": f"加载设置失败: {error_msg}"}

    def exportHomeworkData(self):
        """导出作业数据"""
        try:
            import tkinter as tk
            from tkinter import filedialog
            from datetime import datetime

            # 获取作业数据
            homework_file = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'data', 'homework.json')
            if not os.path.exists(homework_file):
                return {"success": False, "message": "没有找到作业数据"}

            with open(homework_file, 'r', encoding='utf-8') as f:
                homework_data = json.load(f)

            # 创建导出文件名
            timestamp = datetime.now().strftime('%Y%m%d_%H%M%S')
            default_filename = f'homework_backup_{timestamp}.json'

            # 使用文件对话框选择保存位置
            root = tk.Tk()
            root.withdraw()
            file_path = filedialog.asksaveasfilename(
                defaultextension='.json',
                filetypes=[('JSON files', '*.json'), ('All files', '*.*')],
                initialfile=default_filename,
                title='导出作业数据'
            )
            root.destroy()

            if file_path:
                with open(file_path, 'w', encoding='utf-8') as f:
                    json.dump(homework_data, f, ensure_ascii=False, indent=2)
                log(f"作业数据已导出到: {file_path}", "info")
                return {"success": True, "message": f"数据已导出到: {file_path}"}
            else:
                return {"success": False, "message": "用户取消了导出"}

        except Exception as e:
            error_msg = str(e)
            log(f"导出数据失败: {error_msg}", "error")
            return {"success": False, "message": f"导出失败: {error_msg}"}

    def importHomeworkData(self):
        """导入作业数据"""
        try:
            import tkinter as tk
            from tkinter import filedialog

            # 使用文件对话框选择文件
            root = tk.Tk()
            root.withdraw()
            file_path = filedialog.askopenfilename(
                filetypes=[('JSON files', '*.json'), ('All files', '*.*')],
                title='导入作业数据'
            )
            root.destroy()

            if not file_path:
                return {"success": False, "message": "用户取消了导入"}

            # 读取文件
            with open(file_path, 'r', encoding='utf-8') as f:
                imported_data = json.load(f)

            # 验证数据格式
            if not isinstance(imported_data, list):
                return {"success": False, "message": "无效的数据格式"}

            # 保存到作业文件
            homework_file = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'data', 'homework.json')
            with open(homework_file, 'w', encoding='utf-8') as f:
                json.dump(imported_data, f, ensure_ascii=False, indent=2)

            log(f"作业数据已从 {file_path} 导入", "info")
            return {"success": True, "message": "数据导入成功", "data": imported_data}

        except Exception as e:
            error_msg = str(e)
            log(f"导入数据失败: {error_msg}", "error")
            return {"success": False, "message": f"导入失败: {error_msg}"}

    def clearAllData(self):
        """清除所有数据"""
        try:
            # 清除作业数据
            homework_file = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'data', 'homework.json')
            if os.path.exists(homework_file):
                os.remove(homework_file)

            # 清除设置数据
            settings_file = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'data', 'settings.json')
            if os.path.exists(settings_file):
                os.remove(settings_file)

            log("所有数据已清除", "info")
            return {"success": True, "message": "所有数据已清除"}

        except Exception as e:
            error_msg = str(e)
            log(f"清除数据失败: {error_msg}", "error")
            return {"success": False, "message": f"清除失败: {error_msg}"}

    def setAutoStart(self, enabled):
        """设置开机自启动"""
        try:
            import platform
            system = platform.system()

            if system == 'Darwin':  # macOS
                # macOS 使用 launchd
                launch_agents_dir = os.path.expanduser('~/Library/LaunchAgents')
                plist_path = os.path.join(launch_agents_dir, 'com.sectl.assignsticker.plist')

                if enabled:
                    # 创建 plist 文件
                    if not os.path.exists(launch_agents_dir):
                        os.makedirs(launch_agents_dir)

                    app_path = os.path.dirname(os.path.abspath(__file__))
                    plist_content = f'''<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>Label</key>
    <string>com.sectl.assignsticker</string>
    <key>ProgramArguments</key>
    <array>
        <string>/usr/bin/python3</string>
        <string>{os.path.join(app_path, 'main.py')}</string>
    </array>
    <key>RunAtLoad</key>
    <true/>
    <key>KeepAlive</key>
    <false/>
</dict>
</plist>'''
                    with open(plist_path, 'w') as f:
                        f.write(plist_content)

                    # 加载 launchd 任务
                    subprocess.run(['launchctl', 'load', plist_path], check=True)
                    log("已启用开机自启动", "info")
                else:
                    # 禁用开机自启动
                    if os.path.exists(plist_path):
                        subprocess.run(['launchctl', 'unload', plist_path], check=False)
                        os.remove(plist_path)
                    log("已禁用开机自启动", "info")

            elif system == 'Windows':
                import winreg
                key_path = r'Software\Microsoft\Windows\CurrentVersion\Run'
                app_path = os.path.dirname(os.path.abspath(__file__))
                exe_path = os.path.join(app_path, 'main.py')

                try:
                    key = winreg.OpenKey(winreg.HKEY_CURRENT_USER, key_path, 0, winreg.KEY_SET_VALUE)
                    if enabled:
                        winreg.SetValueEx(key, 'AssignSticker', 0, winreg.REG_SZ, f'python "{exe_path}"')
                        log("已启用开机自启动", "info")
                    else:
                        try:
                            winreg.DeleteValue(key, 'AssignSticker')
                            log("已禁用开机自启动", "info")
                        except FileNotFoundError:
                            pass
                    winreg.CloseKey(key)
                except Exception as e:
                    log(f"设置开机自启动失败: {str(e)}", "error")
                    return {"success": False, "message": f"设置失败: {str(e)}"}

            return {"success": True, "message": "设置已更新"}

        except Exception as e:
            error_msg = str(e)
            log(f"设置开机自启动失败: {error_msg}", "error")
            return {"success": False, "message": f"设置失败: {error_msg}"}

    def checkUpdate(self):
        """检查更新"""
        try:
            import urllib.request
            import json as json_module

            # GitHub API 地址
            repo = "SECTL/AssignSticker"
            api_url = f"https://api.github.com/repos/{repo}/releases/latest"

            try:
                request = urllib.request.Request(api_url)
                request.add_header('User-Agent', 'AssignSticker')
                
                with urllib.request.urlopen(request, timeout=10) as response:
                    data = json_module.loads(response.read().decode('utf-8'))
                
                latest_version = data.get('tag_name', 'v1.0.0').lstrip('v')
                current_version = "1.3.0"
                
                # 比较版本
                is_latest = self.compare_versions(current_version, latest_version) >= 0
                
                log(f"检查更新完成：当前版本 {current_version}，最新版本 {latest_version}", "info")
                
                return {
                    "success": True,
                    "isLatest": is_latest,
                    "latestVersion": latest_version,
                    "message": "检查完成" if is_latest else "有新版本可用"
                }
                
            except Exception as e:
                error_msg = str(e)
                log(f"检查更新失败: {error_msg}", "error")
                return {"success": False, "message": f"检查失败: {error_msg}"}

        except Exception as e:
            error_msg = str(e)
            log(f"检查更新异常: {error_msg}", "error")
            return {"success": False, "message": f"检查异常: {error_msg}"}

    def compare_versions(self, v1, v2):
        """比较版本号，返回 0 表示相等，正数表示 v1 > v2，负数表示 v1 < v2"""
        try:
            def parse_version(version):
                parts = version.split('.')
                result = []
                for part in parts:
                    try:
                        result.append(int(part))
                    except ValueError:
                        result.append(0)
                return result
            
            v1_parts = parse_version(v1)
            v2_parts = parse_version(v2)
            
            # 补齐长度
            max_len = max(len(v1_parts), len(v2_parts))
            v1_parts.extend([0] * (max_len - len(v1_parts)))
            v2_parts.extend([0] * (max_len - len(v2_parts)))
            
            # 逐位比较
            for i in range(max_len):
                if v1_parts[i] != v2_parts[i]:
                    return v1_parts[i] - v2_parts[i]
            
            return 0
        except Exception:
            return 0

    def getChangelog(self):
        """获取更新日志"""
        try:
            import urllib.request
            import json as json_module

            repo = "SECTL/AssignSticker"
            api_url = f"https://api.github.com/repos/{repo}/releases/latest"

            try:
                request = urllib.request.Request(api_url)
                request.add_header('User-Agent', 'AssignSticker')
                
                with urllib.request.urlopen(request, timeout=10) as response:
                    data = json_module.loads(response.read().decode('utf-8'))
                
                changelog = data.get('body', '暂无更新日志')
                
                log("获取更新日志成功", "info")
                return {"success": True, "changelog": changelog}
                
            except Exception as e:
                error_msg = str(e)
                log(f"获取更新日志失败: {error_msg}", "error")
                return {"success": False, "message": f"获取失败: {error_msg}"}

        except Exception as e:
            error_msg = str(e)
            log(f"获取更新日志异常: {error_msg}", "error")
            return {"success": False, "message": f"获取异常: {error_msg}"}

    def loadHomeworkData(self):
        """加载作业数据，过滤掉已过期的作业"""
        try:
            from datetime import datetime, timedelta

            homework_file = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'data', 'homework.json')

            if not os.path.exists(homework_file):
                return {"success": True, "data": [], "message": "没有作业数据"}

            with open(homework_file, 'r', encoding='utf-8') as f:
                homework_list = json.load(f)

            # 获取当前时间
            now = datetime.now()

            # 过滤未过期的作业（截止日期当天24:00之前都算未过期）
            valid_homework = []
            expired_homework = []

            for homework in homework_list:
                if homework.get('endTime'):
                    end_time = datetime.fromisoformat(homework['endTime'].replace('Z', '+00:00').replace('+00:00', ''))
                    # 计算截止日期的当天24:00
                    deadline = end_time.replace(hour=23, minute=59, second=59, microsecond=999999)

                    if now <= deadline:
                        valid_homework.append(homework)
                    else:
                        expired_homework.append(homework)
                else:
                    # 没有截止日期的作业默认保留
                    valid_homework.append(homework)

            # 如果有过期作业，更新文件
            if expired_homework:
                with open(homework_file, 'w', encoding='utf-8') as f:
                    json.dump(valid_homework, f, ensure_ascii=False, indent=2)
                log(f"已清理 {len(expired_homework)} 个过期作业", "info")

            return {"success": True, "data": valid_homework, "message": f"加载了 {len(valid_homework)} 个作业"}

        except Exception as e:
            error_msg = str(e)
            log(f"加载作业数据失败: {error_msg}", "error")
            return {"success": False, "message": f"加载失败: {error_msg}", "data": []}

    def saveHomeworkToAutoSave(self, homework_list):
        """自动保存作业到自动保存目录"""
        try:
            auto_save_dir = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'data', 'homework_saves_auto')
            if not os.path.exists(auto_save_dir):
                os.makedirs(auto_save_dir)

            # 生成文件名：时间戳.json
            timestamp = datetime.now().strftime('%Y%m%d_%H%M%S')
            filename = f"auto_save_{timestamp}.json"
            filepath = os.path.join(auto_save_dir, filename)

            # 写入JSON文件
            with open(filepath, 'w', encoding='utf-8') as f:
                json.dump(homework_list, f, ensure_ascii=False, indent=2)

            log(f"作业已自动保存到: {filepath}", "info")
            return {"success": True, "message": f"自动保存成功: {filename}"}
        except Exception as e:
            error_msg = str(e)
            log(f"自动保存作业失败: {error_msg}", "error")
            return {"success": False, "message": f"自动保存失败: {error_msg}"}

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
                
                # 加载并应用缩放设置
                try:
                    settings_file = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'data', 'settings.json')
                    if os.path.exists(settings_file):
                        with open(settings_file, 'r', encoding='utf-8') as f:
                            settings = json.load(f)
                        
                        # 应用缩放设置
                        if 'zoom' in settings:
                            zoom = settings['zoom']
                            main_window.evaluate_js(f"document.body.style.zoom = '{zoom}%'")
                            log(f"应用缩放设置: {zoom}%", "info")
                except Exception as e:
                    log(f"加载缩放设置失败: {str(e)}", "error")

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
