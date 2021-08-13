#!/usr/bin/env python3

import os
import shutil
import sys
from subprocess import run

CURDIR = os.path.abspath(os.path.dirname(__file__))
NPM = shutil.which("npm")

run([NPM, "run-script"] + sys.argv[1:], cwd=CURDIR)
