#!/usr/bin/env python3

import os
import shutil
from pathlib import Path

from genvid.toolbox import BaseTool


class Builder(BaseTool):
    NAME = "web"
    DESCRIPTION = "Build the web site."

    def __init__(self):
        super().__init__()
        self.projectdir = Path(__file__).absolute().parent
        self.modulesdir = self.projectdir / "modules"
        self.nodemodulesdir = self.projectdir / "node_modules"
        self.packagelockfile = self.projectdir / "package-lock.json"

    def copy_api(self, packageFilePath: Path):
        """This method copies the genvid and genvid-math packages from the sdk install dir.
        
        This avoids the package to be created with a symlink in it, and permits an easier
        building of the docker image.
        """

        packageFile = packageFilePath.name
        destPath = self.modulesdir / packageFile

        shutil.copy(str(packageFilePath), str(destPath))

    def prepare(self):
        sdkweb = Path(self.ROOTDIR, "api", "web", "dist", "genvid.tgz")
        sdkmathweb = Path(self.ROOTDIR, "api", "web", "dist",
                          "genvid-math.tgz")
        if self.modulesdir.exists():
            self.rmtree(self.modulesdir)
        if self.nodemodulesdir.exists():
            self.rmtree(self.nodemodulesdir)
        if self.packagelockfile.exists():
            os.remove(self.packagelockfile)        
        self.modulesdir.mkdir()
        self.copy_api(sdkmathweb)
        self.copy_api(sdkweb)

    def build(self):
        npm = self.which("npm")
        self.run(npm, "install", cwd=self.projectdir)
        self.run(npm, "run", "build", cwd=self.projectdir)

    def all(self):
        self.prepare()
        self.build()

    def add_commands(self):
        self.add_command("prepare", "Copy the SDK inside the project.")
        self.add_command("build", "Build the current project.")
        self.add_command("all", "Prepare and build the current project.")

    def run_command(self, command, options):
        getattr(self, command)()


def get_parser():
    return Builder().get_parser()


def main():
    Builder().main()


if __name__ == "__main__":
    os.environ.setdefault("GENVID_TOOLBOX_LOGLEVEL", "INFO")
    exit(main() or 0)
