#!/usr/bin/env python3
import os
import subprocess
import hashlib
from typing import List
from pathlib import Path
from shutil import move, make_archive
from collections import OrderedDict
import hcl
from genvid.toolbox import (
    ConsulTemplateTool,
    SDK,
    Profile,
    DockerTool
)
class GenvidProject(ConsulTemplateTool, DockerTool):
    def __init__(self):
        super().__init__()
        self.current_dir = Path(__file__).absolute().parent
        self.images_dir_default = self.current_dir / "images"
        self._sdk = None
        self.cluster_id = 'local'
        self.config = None
        self.config_file = self.current_dir / "project-config.hcl"
        self.NAME = None
        self.DESCRIPTION = None
        self.CONFIG_FILES = OrderedDict()
    def readconfig(self, path):
        with open(path, 'r') as fp:
            return hcl.load(fp)
    @property
    def sdk(self) -> SDK:
        if self._sdk is None:
            self._sdk = SDK(cluster_id=self.cluster_id)
        return self._sdk
    def package(self, folder:bool, docker:bool, name:str, path:str, dest:str):
        if folder:
            self.package_folder(name, path, dest)
        elif docker:
            self.package_docker(name, path, dest)
    def package_folder(self, name:str, path:str, dest:str):
        dest = Path(dest)
        dest.mkdir(exist_ok=True)
        path = Path(path)
        if not path.is_dir():
            self.logger.error("%s doesn't exist.", path)
            return
        packageName = '_'.join([name, path.name, self.GENVID_TOOLBOX_VERSION])
        newarchive = dest.joinpath(packageName)
        oldarchives = dest.glob(packageName + "_*.zip")
        for old in oldarchives:
            self.logger.info("Deleting " + str(old))
            old.unlink()
        self.logger.info("Packaging %s",path)
        zipname = make_archive(
            base_name=str(newarchive),
            format='zip',
            root_dir=str(path),
            logger=self.logger)
        with open(zipname, 'rb') as zfile:
            sha256 = hashlib.sha256(zfile.read())
        shazipname = dest.joinpath("{appdir}_{sha256}.zip".format(
                appdir=packageName,
                sha256=sha256.hexdigest()[:12]))
        move(zipname, str(shazipname))
        self.logger.info("New archive %s", shazipname)
    def package_docker(self, name:str, path:str, dest:str):
        dest = Path(dest)
        dest.mkdir(exist_ok=True)
        path = Path(path)
        if not path.is_dir():
            self.logger.error("%s doesn't exist.", path)
            return
        self.logger.info("Dockerizing %s", path)
        packageName = '_'.join([name, path.name])
        oldarchives = dest.glob(packageName + "_*.zip")
        for old in oldarchives:
            self.logger.info("Deleting " + str(old))
            old.unlink()
        tag = self.version_to_imagetag(packageName, self.GENVID_TOOLBOX_VERSION)
        dockerfile = path / "Dockerfile"
        if not dockerfile.exists():
            self.logger.error("%s doesn't exist.", dockerfile)
            return
        self.logger.info("Building %s from %s", tag, dockerfile)
        self.build_docker(tag, path=str(path.resolve()))
        self.logger.info("Saving %s", tag)
        self.update_image(packageName,
                          self.GENVID_TOOLBOX_VERSION,
                          clean=True,
                          compress=True,
                          basedir=str(dest))
    def get_template(self, name):
        folder = "local" if self.cluster_id == "local" else "cloud"
        template_path = self.current_dir.joinpath("templates", folder,
                                     name + ".nomad.tmpl")
        if not template_path.exists():
            return ""
        with open(str(template_path)) as template_file:
            return template_file.read()
    def get_config(self, target: str, required: bool = True) -> dict:
        for ext in (".hcl", ".json"):
            file_path = self.current_dir.joinpath("config", target + ext)
            if file_path.exists():
                break
        if not file_path.exists() and required:
            self.logger.error("%s config file doesn't exist.", file_path)
            # trigger an error by opening the file that doesn't exist
        elif not file_path.exists():
            return {}
        self.logger.info("Loading configuration from %s", file_path)
        env = os.environ.copy()
        env.setdefault("PROJECTDIR", str(self.current_dir))
        try:
            process = self.consul_template_once(use_consul=False,
                                       use_vault=False,
                                       env=env,
                                       template=file_path,
                                       dry=True)
        except subprocess.SubprocessError as ex:
            error_message = ex.stderr.decode()
            self.logger.error("Consul template has return an error:\n" +
                              error_message)
            raise
        return process.configuration
    def merge_config(self, targets: List[str]):
        targets = targets if targets else self.CONFIG_FILES.keys()
        config = OrderedDict()
        for key in self.CONFIG_FILES.keys():
            if key in targets:
                partial = self.get_config(key, self.CONFIG_FILES[key]["required"])
                self.sdk.merge_dict(partial, config)
        return config
    def load(self, targets: List[str]):
        config = self.merge_config(targets)
        # Load the raw template into the configuration
        for key, job in config.setdefault("job", {}).items():
            job["template"] = self.get_template(key)
        self.sdk.set_config(config)
    def unload(self, targets: List[str]):
        config = self.merge_config(targets)
        self.sdk.remove_config(config)
    COMMANDS = {
        "package": "Prepare files to be deployed",
        "load": "Load the specified target definition in the cloud",
        "unload": "Unload the specified target definition in the cloud"
    }
    def add_commands(self):
        default_configs = sorted(self.CONFIG_FILES.keys())
        self.parser.add_argument(
            "-c", "--cluster_id",
            default=self.cluster_id,
            help="The cluster id (default: %(default)s).")
        for command, help_text in self.COMMANDS.items():
            parser = self.add_command(command, help_text)
            if command == "package":
                group = parser.add_mutually_exclusive_group(required=True)
                group.add_argument(
                    "--docker",
                    action='store_true',
                    help="Package the path using docker")
                group.add_argument(
                    "--folder",
                    action='store_true',
                    help="Package the path using zip")
                parser.add_argument(
                    "-n",
                    "--name",
                    default=self.current_dir.name,
                    help="The package name (default: %(default)s)")
                parser.add_argument(
                    "-p",
                    "--path",
                    help="Paths to package.",
                    required=True)
                parser.add_argument(
                    "-d",
                    "--dest",
                    default=self.ARTIFACTS_DIR,
                    help="Destination folder (default: %(default)s).")
            if command in ("load", "unload"):
                parser.add_argument(
                    "targets",
                    help="The configuration to load or unload",
                    nargs="*",
                    choices=default_configs + [[]])
    def run_command(self, command, options):
        method = command.replace("-", "_")
        self.cluster_id = options.cluster_id or "local"
        del options.cluster_id
        return getattr(self, method)(**vars(options))
    def load_config(self):
        if not self.config_file.exists():
            self.logger.error("Configuration file %s does not exist" % self.config_file)
            s = ('// Project Name\n'
                'project "<ProjectName>" {\n'
                '\n'
                '    // Enter a short description of the project\n'
                '    description = "<ProjectDescription>"\n'
                '\n'
                '    // Enter one or many configuration file matching the ones in config folder\n'
                '    // Setting required to true means that the file requires a template module\n'
                '    config "stream" { required=true }\n'
                '    config "events" { required=true }\n'
                '    config "web" { required=true }\n'
                '    config "app" { required=true }\n'
                '}')
            with open(str(self.config_file), 'w') as config:
                config.write(s)
            self.logger.info("Please review %s to setup the project" % self.config_file)
            return False
        self.config = self.readconfig(str(self.config_file))
        self.projects = self.config["project"]
        # Extract the first name as we assume there is only one project
        self.NAME = list(self.projects.keys())[0]
        self.project = self.projects[self.NAME]
        self.DESCRIPTION = self.project["description"]
        self.CONFIG_FILES = OrderedDict()
        for cfg in self.project["config"]:
            name = list(cfg.keys())[0]
            self.CONFIG_FILES[name] = cfg[name]
        return True
def get_parser():
    tool = GenvidProject()
    if not tool.load_config():
        return 1
    return tool.get_parser()
def main():
    profile = Profile()
    profile.apply()
    os.environ.setdefault("GENVID_TOOLBOX_LOGLEVEL", "INFO")
    tool = GenvidProject()
    if not tool.load_config():
        return 1
    return tool.main(profile=profile)
if __name__ == "__main__":
    exit(main() or 0)