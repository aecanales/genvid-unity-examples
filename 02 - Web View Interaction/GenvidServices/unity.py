#!/usr/bin/env python3

import os
from pathlib import Path
from typing import List

from genvid.toolbox import SDK, ConsulTemplateTool, Profile


class UnitySample(ConsulTemplateTool):
    NAME = "Unity sample"
    DESCRIPTION = "Unity sample script"

    CONFIG_FILES = [
        dict(name="sample", required=True),
        dict(name="stream", required=True),
        dict(name="events", required=True),
        dict(name="game", required=True),
        dict(name="web", required=True),
    ]
    "The configuration files to load in order. The order is important as some file may override some values"

    IMAGES = dict(game="unity_dev_", web="unityweb_dev_")
    "The list of images to upload"

    def __init__(self):
        super().__init__()
        self.base_dir = os.path.dirname(os.path.abspath(__file__))
        self.images_dir = os.path.join(self.base_dir, "images")
        self._sdk = None
        self.cluster_id = "local"

    @property
    def sdk(self) -> SDK:
        if self._sdk is None:
            self._sdk = SDK(cluster_id=self.cluster_id)
        return self._sdk

    def env(self):
        self.print_env()
        print("---Additional Environment Variables---")
        if (os.getenv('UNITYPROJECTROOT') == None):
            print(
                "*** Please set your UNITYPROJECTROOT environment variable to the directory of your project folder"
            )
            print(
                "*** Powershell example:\n*** $env:UNITYPROJECTROOT = 'C:\\Users\\[MyUserName]\\UnityProjects\\[MyProjectFolder]'\n"
            )
            exit()
        else:
            print('UNITYPROJECTROOT=' + os.getenv('UNITYPROJECTROOT'))

    def pyrun(self, *args, **kwargs):
        env = os.environ.copy()
        del env["CURDIR"]
        super().pyrun(*args, env=env)

    def unity_build(self, target: str, debug: bool, bit32: bool,
                    game_dir: str):
        args = []
        if debug:
            args += ["--conf", "Debug"]
        if bit32:
            args += ["--plat", "32"]
        self.pyrun(Path(self.base_dir, "run.py"), target, "-g", game_dir,
                   *args)

    def build(self, targets: List[str], debug: bool, bit32: bool,
              game_dir: str):
        targets = targets if targets else ["game", "web"]
        if (os.getenv('UNITYPROJECTROOT') == None):
            print(
                "*** Please set your UNITYPROJECTROOT environment variable to the directory of your project folder"
            )
            print(
                "*** Powershell example:\n*** $env:UNITYPROJECTROOT = 'C:\\Users\\[MyUserName]\\UnityProjects\\[MyProjectFolder]'\n"
            )
            exit()
        print("ENV: UNITYPROJECTROOT = " + os.getenv('UNITYPROJECTROOT'))
        targets = targets if targets else ["game", "web"]
        if "game" in targets:
            self.unity_build("build", debug, bit32, game_dir)
        if "web" in targets:
            print("self.pyrun web build.py")
            self.pyrun(os.path.join(self.base_dir, "web", "build.py"), "all")

    def get_template(self, name):
        folder = "local" if self.cluster_id == "local" else "cloud"
        template_path = os.path.join(self.base_dir, "templates", folder,
                                     name + ".nomad.tmpl")
        with open(template_path) as template_file:
            return template_file.read()

    def get_config(self, target: str, required: bool = True) -> dict:
        for ext in (".hcl", ".json"):
            file_path = os.path.join(self.base_dir, "config", target + ext)
            if os.path.exists(file_path):
                break
        else:
            if not required:
                return {}
            elif target == "stream":
                self.logger.error("stream config file doesn't exist.")
                # trigger an error by opening the file that doesn't exist
                open(file_path)

        env = os.environ.copy()
        env.setdefault("PROJECTDIR", self.base_dir)
        return self.sdk.load_config_template(file_path, env=env)

    def merge_config(self, targets: List[str]):
        targets = targets if targets else [
            f["name"] for f in self.CONFIG_FILES
        ]

        config = {}
        for config_file in self.CONFIG_FILES:
            name = config_file["name"]
            required = config_file["required"]
            if name in targets:
                partial = self.get_config(name, required)
                self.sdk.merge_dict(partial, config)

        return config

    def prepare(self):
        self.pyrun(Path(self.base_dir, "run.py"), "prepare")
        self.pyrun(Path(self.base_dir, "web", "build.py"), "prepare")

    COMMANDS = {
        "env": "Print environment variables",
        "build": "Build the specified target",
        # Disabled 'Prepare' for users, since the Genvid.dll files already exist in the project
        #"prepare": "Install the Genvid package into the sample project",
    }

    def add_commands(self):
        default_targets = sorted([f['name'] for f in self.CONFIG_FILES])
        self.parser.add_argument("-c",
                                 "--cluster_id",
                                 help="The cluster id. Default local")
        for command, help_text in self.COMMANDS.items():
            parser = self.add_command(command, help_text)
            if command in ("build"):
                parser.add_argument("targets",
                                    help="The targets to build",
                                    nargs="*")
                parser.add_argument("-d",
                                    "--debug",
                                    action="store_true",
                                    help="Build the game in debug")
                parser.add_argument("-b",
                                    "--bit32",
                                    action="store_true",
                                    help="Build the game in 32bit")
                parser.add_argument(
                    "-g",
                    "--game-dir",
                    default=os.getenv(
                        'UNITYPROJECTROOT'
                    ),  # was previously hardcoded value: "app"
                    help="The game directory (relative to the current directory)"
                    " (default: %(default)s)")

    def run_command(self, command, options):
        method = command.replace("-", "_")
        self.cluster_id = options.cluster_id or "local"
        del options.cluster_id
        return getattr(self, method)(**vars(options))


def get_parser():
    tool = UnitySample()
    return tool.get_parser()


def main():
    profile = Profile()
    profile.apply()

    tool = UnitySample()
    return tool.main(profile=profile)


if __name__ == '__main__':
    import logging

    logging.basicConfig(level=logging.INFO)
    exit(main() or 0)
