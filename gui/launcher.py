"""Launcher: execute multi_play_d2r.bat via subprocess."""

import os
import subprocess
import sys


class Launcher:
    def __init__(self, config_dir):
        self.config_dir = config_dir
        # The bat file is at the repo root (parent of config/)
        self.repo_root = os.path.dirname(config_dir)
        self.bat_path = os.path.join(self.repo_root, "multi_play_d2r.bat")

    def launch(self):
        if not os.path.isfile(self.bat_path):
            raise FileNotFoundError(f"Launcher script not found: {self.bat_path}")

        # Launch in a new visible cmd window so the user can see output
        subprocess.Popen(
            ["cmd", "/c", "start", "D2R Launcher", "cmd", "/c", self.bat_path],
            cwd=self.repo_root,
            shell=False,
        )
