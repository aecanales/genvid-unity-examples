#!/usr/bin/env python3

import os
import winreg
import zipfile
from pathlib import Path

from genvid.toolbox import SDK, Profile


class UnityTool(SDK):

    NAME = "unity"
    DESCRIPTION = "Unity Sample Build Utility"

    class CHOICES:
        CONF = ("Debug", "Release")
        PLAT = ("32", "64")

    class DEFAULTS:
        CONF = 'Release'
        PLAT = '64'
        GAMEDIR = "app"

    TO_PLAT = {"32": "x86", "64": "x64"}

    COMMANDS = {
        "prepare": "Import the Genvid Unity package into the game.",
        "build": "Build the Unity sample",
    }

    def find_unity_in_registry(self, options):
        # Registry
        for hkey in (winreg.HKEY_CURRENT_USER, winreg.HKEY_LOCAL_MACHINE):
            try:
                with winreg.OpenKey(
                        hkey,
                        r"SOFTWARE\Unity Technologies\Installer\Unity") as key:
                    try:
                        if options == "x86":
                            install, regtype = winreg.QueryValueEx(
                                key, "Location")
                        else:
                            install, regtype = winreg.QueryValueEx(
                                key, "Location x64")

                        if regtype == winreg.REG_SZ:
                            candidate = os.path.join(install, "Editor",
                                                     "Unity.exe")
                            if os.path.isfile(candidate):
                                return candidate
                    except FileNotFoundError:
                        pass
            except FileNotFoundError:
                pass
        return None

    def find_unity(self, options):
        candidate = self.find_unity_in_registry(options)
        if candidate is not None:
            return candidate
        for pfenv in ('ProgramFiles', 'ProgramFiles(x86)'):
            pfpath = os.environ.get(pfenv)
            if not pfpath: continue
            candidate = os.path.join(pfpath, "Unity", "Editor", "Unity.exe")
            if os.path.isfile(candidate):
                return candidate
        # Assume it's on the path, and let the command fail otherwise.
        return self.which("Unity.exe")

    def __init__(self):
        super(UnityTool, self).__init__()
        curdir = os.path.dirname(os.path.abspath(__file__))
        self.CURRENT_DIR = curdir
        self.UNITY = self.setdefault("UNITY", "")
        if not self.UNITY:
            self.UNITY = self.find_unity("x64")
        self.IMAGES_DIR = os.path.join(curdir, "images")

    def run_unity(self, *args, **kwargs):
        try:
            self.run(self.UNITY, *args, **kwargs)
        finally:
            logfile = Path(os.environ["LOCALAPPDATA"], "Unity", "Editor",
                           "Editor.log")
            try:
                self.safe_print(logfile.read_text())
            except:
                self.logger.exception("Error accessing logfile: %s", logfile)

    def prepare(self, options):
        print("Install Genvid Unity package")

        appdir = os.path.join(self.CURRENT_DIR, options.game_dir)

        cmd = [
            "-projectPath",
            appdir,
            # Doesn't work: Unity complain script errors (no GenvidSDK symbols)
            # and decide to stop there.
            # Without the batchmode, the GUI is displayed but the import can complete.
            # "-batchmode",
            # Using nographics here make Unity complain about not having the permission
            # to set the license file.
            # "-nographics",
            "-importPackage",
            options.unitypackage,
            "-logFile",
            "-quit",
        ]

        self.run_unity(*cmd, cwd=appdir)

    def build(self, options):
        print("Build Unity")

        appdir = os.path.join(self.CURRENT_DIR, options.game_dir)
        platform = self.TO_PLAT[options.plat]
        method = "Build.BuildGame" + platform + "_" + options.conf

        cmd = [
            "-projectPath",
            appdir,
            "-executeMethod",
            method,
            "-batchmode",
            "-logFile",
            "-quit",
        ]
        self.run_unity(*cmd, cwd=appdir)

    def add_commands(self):
        unitypackage = os.path.join(self.ROOTDIR, "engine-integration",
                                    "unity", "genvid.unitypackage")
        for cmd, desc in sorted(self.COMMANDS.items()):
            parser = self.add_command(cmd, desc)
            if cmd == "update-image":
                continue
            parser.add_argument(
                "-u",
                "--unitypackage",
                default=unitypackage,
                help="The Genvid Package to import (def: %(default)s)")
            parser.add_argument(
                "-g",
                "--game-dir",
                default=self.DEFAULTS.GAMEDIR,
                help="The game directory (relative to the current directory)"
                " (def: %(default)s)")
            parser.add_argument("-c",
                                "--conf",
                                default=self.DEFAULTS.CONF,
                                choices=self.CHOICES.CONF,
                                help="The configuration (def: %(default)s)")
            parser.add_argument("-p",
                                "--plat",
                                choices=self.CHOICES.PLAT,
                                default=self.DEFAULTS.PLAT,
                                help="The platform (def: %(default)s)")
            parser.add_argument("-cl",
                                "--clean",
                                help="Clean the build before building it",
                                action="store_true")

    def run_command(self, command, options):
        method = command.replace("-", "_")
        return getattr(self, method)(options)


def get_parser():
    tool = UnityTool()
    return tool.get_parser()


def main():
    profile = Profile()
    profile.apply()

    tool = UnityTool()
    return tool.main(profile=profile)


if __name__ == '__main__':
    parent = os.path.dirname
    rootdir = parent(parent(parent(os.path.abspath(__file__))))
    UnityTool.setrootdir(rootdir)
    UnityTool.setbasescript(__file__)
    exit(main() or 0)
