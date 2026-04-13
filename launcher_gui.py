#!/usr/bin/env python3
"""D2R Multi-Play Launcher GUI - Entry Point."""

import os
import sys


def main():
    # Determine config directory: relative to this script's location
    if getattr(sys, "frozen", False):
        # Running as PyInstaller exe
        base_dir = os.path.dirname(sys.executable)
    else:
        base_dir = os.path.dirname(os.path.abspath(__file__))

    config_dir = os.path.join(base_dir, "config")

    # Ensure config dir exists
    if not os.path.isdir(config_dir):
        os.makedirs(config_dir, exist_ok=True)

    from gui.main_window import MainWindow
    app = MainWindow(config_dir)
    app.run()


if __name__ == "__main__":
    main()
