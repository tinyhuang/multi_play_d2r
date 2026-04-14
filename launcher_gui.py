#!/usr/bin/env python3
"""D2R Multi-Play Launcher GUI - Entry Point."""

import os
import sys
import traceback


def main():
    # Determine config directory: relative to this script's location
    if getattr(sys, "frozen", False):
        # Running as PyInstaller exe
        base_dir = os.path.dirname(sys.executable)
    else:
        base_dir = os.path.dirname(os.path.abspath(__file__))

    # Ensure the script's directory is on sys.path so 'gui' package can be found
    if base_dir not in sys.path:
        sys.path.insert(0, base_dir)

    config_dir = os.path.join(base_dir, "config")

    # Ensure config dir exists
    if not os.path.isdir(config_dir):
        os.makedirs(config_dir, exist_ok=True)

    from gui.main_window import MainWindow
    app = MainWindow(config_dir)
    app.run()


if __name__ == "__main__":
    try:
        main()
    except Exception:
        err = traceback.format_exc()
        # Try to show the error in a GUI messagebox
        try:
            import tkinter as tk
            from tkinter import messagebox
            root = tk.Tk()
            root.withdraw()
            messagebox.showerror("D2R Launcher Error", err)
            root.destroy()
        except Exception:
            pass
        print(err, file=sys.stderr)
        input("Press Enter to exit...")
