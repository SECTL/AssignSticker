import argparse
import os
import sys


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--signal-file", required=True)
    parser.add_argument("--logo", required=True)
    args = parser.parse_args()

    try:
        from PySide6.QtCore import QPoint, Qt
        from PySide6.QtGui import QPixmap
        from PySide6.QtWidgets import (
            QApplication,
            QFrame,
            QHBoxLayout,
            QLabel,
            QWidget,
        )
    except Exception as e:
        print(f"PySide6 import failed: {e}")
        return 1

    class WidgetWindow(QWidget):
        def __init__(self):
            super().__init__()
            self._drag_pos = None

            self.setWindowTitle("AssignSticker Widget")
            self.setFixedSize(80, 80)
            self.setWindowFlags(
                Qt.FramelessWindowHint | Qt.WindowStaysOnTopHint | Qt.Tool
            )
            self.setAttribute(Qt.WA_TranslucentBackground, True)

            root = QHBoxLayout(self)
            root.setContentsMargins(0, 0, 0, 0)

            card = QFrame()
            card.setObjectName("card")
            card_layout = QHBoxLayout(card)
            card_layout.setContentsMargins(12, 12, 12, 12)
            card_layout.setSpacing(0)

            logo_label = QLabel()
            logo_label.setFixedSize(56, 56)
            if os.path.exists(args.logo):
                pix = QPixmap(args.logo).scaled(
                    56, 56, Qt.KeepAspectRatioByExpanding, Qt.SmoothTransformation
                )
                logo_label.setPixmap(pix)
            logo_label.setCursor(Qt.PointingHandCursor)

            logo_label.mousePressEvent = lambda e: self.on_show_clicked() if e.button() == Qt.LeftButton else None

            card_layout.addWidget(logo_label, 0, Qt.AlignCenter)
            root.addWidget(card)

            self.setStyleSheet(
                """
                #card {
                    background: rgba(255, 255, 255, 0.96);
                    border: 1px solid rgba(88, 112, 165, 0.25);
                    border-radius: 18px;
                }
                """
            )

        def on_show_clicked(self):
            try:
                with open(args.signal_file, "w", encoding="utf-8") as f:
                    f.write("show")
            except Exception:
                pass
            self.close()

        def mousePressEvent(self, event):
            if event.button() == Qt.LeftButton:
                self._drag_pos = event.globalPosition().toPoint() - self.frameGeometry().topLeft()
                event.accept()

        def mouseMoveEvent(self, event):
            if self._drag_pos and event.buttons() & Qt.LeftButton:
                self.move(event.globalPosition().toPoint() - self._drag_pos)
                event.accept()

        def mouseReleaseEvent(self, event):
            self._drag_pos = None
            event.accept()

    app = QApplication(sys.argv)
    window = WidgetWindow()

    screen = app.primaryScreen()
    if screen:
        geo = screen.availableGeometry()
        x = geo.right() - window.width() - 24
        y = geo.bottom() - window.height() - 80
        window.move(QPoint(x, y))

    window.show()
    return app.exec()


if __name__ == "__main__":
    raise SystemExit(main())
