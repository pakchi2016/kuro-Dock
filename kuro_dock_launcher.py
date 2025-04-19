import sys
import os
import json
import subprocess
from PyQt5 import QtWidgets, QtGui, QtCore

CONFIG_FILE = "launcher_config.json"

class AppLauncherButton(QtWidgets.QPushButton):
    def __init__(self, app_data, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self.app_data = app_data
        self.setIcon(QtGui.QIcon(app_data['icon'] or ''))
        self.setIconSize(QtCore.QSize(48, 48))
        self.setToolTip(app_data['name'])
        self.clicked.connect(self.launch_app)

    def launch_app(self):
        cmd = [self.app_data['path']]
        if self.app_data['args']:
            cmd.extend(self.app_data['args'])
        subprocess.Popen(cmd, shell=True)

class KuroDock(QtWidgets.QWidget):
    def __init__(self):
        super().__init__()
        self.setWindowTitle("Kuro-Dock")
        self.setWindowFlags(
            QtCore.Qt.FramelessWindowHint |
            QtCore.Qt.WindowStaysOnTopHint |
            QtCore.Qt.Tool
        )
        self.setAcceptDrops(True)
        self.setStyleSheet("background-color: #1e1e1e; border: 1px solid #444;")
        
        screen = QtWidgets.QApplication.primaryScreen().size()
        self.setGeometry(screen.width() - screen.width() // 10, 0, screen.width() // 10, screen.height())

        self.layout = QtWidgets.QVBoxLayout()
        self.layout.setContentsMargins(2, 20, 2, 2)
        self.setLayout(self.layout)

        self.drag_position = None

        self.load_config()

    def load_config(self):
        if not os.path.exists(CONFIG_FILE):
            self.apps = []
        else:
            with open(CONFIG_FILE, 'r', encoding='utf-8') as f:
                self.apps = json.load(f)

        for app in self.apps:
            self.add_app_button(app)

    def save_config(self):
        with open(CONFIG_FILE, 'w', encoding='utf-8') as f:
            json.dump(self.apps, f, indent=2, ensure_ascii=False)

    def add_app_button(self, app_data):
        btn = AppLauncherButton(app_data)
        self.layout.addWidget(btn)

    def dragEnterEvent(self, event):
        if event.mimeData().hasUrls():
            event.acceptProposedAction()

    def dropEvent(self, event):
        for url in event.mimeData().urls():
            path = url.toLocalFile()
            if os.path.isfile(path):
                name = os.path.basename(path)
                icon = ""  # アイコンファイルのパス（今後拡張予定）
                args, ok = QtWidgets.QInputDialog.getText(self, "引数入力", f"{name} の引数（空でもOK）:")
                app_data = {
                    "name": name,
                    "path": path,
                    "args": args.split() if args else [],
                    "icon": icon
                }
                self.apps.append(app_data)
                self.add_app_button(app_data)
        self.save_config()

    def mousePressEvent(self, event):
        if event.button() == QtCore.Qt.LeftButton:
            self.drag_position = event.globalPos() - self.frameGeometry().topLeft()

    def mouseMoveEvent(self, event):
        if self.drag_position and event.buttons() == QtCore.Qt.LeftButton:
            new_pos = event.globalPos() - self.drag_position
            screen = QtWidgets.QApplication.primaryScreen().size()
            if new_pos.x() < screen.width() // 2:
                self.move(0, 0)
            else:
                self.move(screen.width() - self.width(), 0)

    def mouseReleaseEvent(self, event):
        self.drag_position = None

if __name__ == '__main__':
    app = QtWidgets.QApplication(sys.argv)
    launcher = KuroDock()
    launcher.show()
    sys.exit(app.exec_())
