# Public domain license.
# Author: igor.zavoychinskiy@gmail.com
# Version: 1.6 (Dec 4nd, 2016)

# A very simple script to produce a .ZIP archive with the product distribution.

import getopt
import glob
import json
import logging
import os
import os.path
import re
import shutil
import subprocess
import sys
import time
import collections

from distutils import dir_util


# ADJUST BEFORE RUN!
# Set it to the local system path.
SHELL_ZIP_BINARY = 'L:/Program Files/7-Zip/7z.exe'

# An executable which will be called to build the project's binaraies in release mode.
SHELL_COMPILE_BINARY_SCRIPT = 'make_binary.cmd'

# For information only.
PACKAGE_TITLE = 'Kerbal Attachment System'

# SRC configs.
SRC = '..'
# Extract version number from here. See ExtractVersion() method.
SRC_VERSIONS_FILE = SRC + '/Source/Properties/AssemblyInfo.cs'
# Path to the release's binary. If it doesn't exist then no release.
SRC_COMPILED_BINARY = SRC + '/Source/bin/Release/KIS.dll'

# DEST configs.
# A path where releaae structure will be constructed.
DEST = '../Release'
# A path to place resulted ZIP file. It must exist.
DEST_RELEASES = '..'
# A format string which accepts VERSION as argument and return distribution
# file name with no extension.
DEST_RELEASE_NAME_FMT = 'KIS_v%d.%d.%d'
# A file name format for releases with build field other than zero.
DEST_RELEASE_NAME_WITH_BUILD_FMT = 'KIS_v%d.%d.%d_build%d'

# Sources to be updated post release (see UpdateVersionInSources).
# All paths must be full.
SRC_REPOSITORY_VERSION_FILE = SRC + '/KIS.version'

# Targets to be updated post release (see PostBuildCopy).
# First item of the tuple sets source, and the second item sets the target.
# Both paths are OS paths (i.e. either absolute or relative).
POST_BUILD_COPY = [
]

# Key is a path in DEST. The path *must* start from "/". The root in this case
# is DEST. There is no way to setup real root.
# Value is a path in SRC. It's either a string or a list of patterns:
# - If value is a plain string then then it's path to a single file or
#   directory.
#   If path designates a folder then the entire tree will be copied.
# - If value is a list then each item:
#   - If does *not* end with "/*" then it's a path to a file.
#   - If *does* end with "/*" then it's a folder name. Only files in the
#     folder are copied, not the whole tree.
#   - If starts from "-" then it's a request to *drop* files in DEST folder
#     (the key). Value is a regular OS path pattern.
STRUCTURE = collections.OrderedDict({
  '/GameData' : [
    '/Binaries/ModuleManager.2.7.5.dll',
  ],
  '/GameData/CommunityCategoryKit' : '/Binaries/CommunityCategoryKit',
  '/GameData/KIS' : [
    '/LICENSE.md',
    '/User Guide.pdf',
    '/settings.cfg',
  ],
  '/GameData/KIS/Parts' : '/Parts',
  '-/GameData/KIS/Parts' : '/fun_*',
  '/GameData/KIS/Sounds' : '/Sounds',
  '/GameData/KIS/Textures' : '/Textures',
  '/GameData/KIS/Plugins' : [
    '/KIS.version',
    '/Binaries/MiniAVC.dll',
    '/Binaries/KSPDev_Utils.dll',
    '/Binaries/KSPDev_Utils_License.md',
    '/Source/bin/Release/KIS.dll',
  ],
  '/GameData/KIS/Patches' : '/Patches',
})

VERSION = None

# Argument values.
MAKE_PACKAGE = False
OVERWRITE_PACKAGE = False


def CopyByRegex(src_dir, dst_dir, pattern):
  for name in os.listdir(src_dir):
    if name == 'CVS':
      continue
    src_file_path = os.path.join(src_dir, name)
    if os.path.isfile(src_file_path) and re.search(pattern, name):
      print 'Copying:', src_file_path
      shutil.copy(src_file_path, dst_dir)


# Makes the binary.
def CompileBinary():
  if not SRC_COMPILED_BINARY is None:
    binary_path = SRC_COMPILED_BINARY
    if os.path.exists(binary_path):
      os.unlink(binary_path)
  print 'Compiling the sources in PROD mode...'
  code = subprocess.call([SHELL_COMPILE_BINARY_SCRIPT])

  if (code != 0
      or not SRC_COMPILED_BINARY is None
      and not os.path.exists(SRC_COMPILED_BINARY)):
    print 'ERROR: Compilation failed.'
    exit(code)


# Purges any existed files in the release folder.
def CleanupReleaseFolder():
  print 'Cleanup release folder...'
  shutil.rmtree(DEST, True)


# Creates whole release structure and copies the required files.
def MakeFoldersStructure():
  print 'Make release folders structure...'
  folders = sorted(STRUCTURE.keys(), key=lambda x: x[0] != '-' and x or x[1:] + 'Z')
  # Make.
  for folder in folders:
    if folder.startswith('-'):
      # Drop files/directories.
      del_path = DEST + folder[1:] + STRUCTURE[folder]
      print 'Drop targets by pattern: %s' % del_path
      for file_name in glob.glob(del_path):
        if os.path.isfile(file_name):
          print 'Dropping file "%s"' % file_name
          os.unlink(file_name)
        else:
          print 'Dropping directory "%s"' % file_name
          shutil.rmtree(file_name, True)
      continue

    # Copy files.
    dest_path = DEST + folder 
    dir_util.mkpath(dest_path)
    sources = STRUCTURE[folder]
    if not isinstance(sources, list):
      src_path = SRC + sources
      print 'Copying folder "%s" into "%s"' % (src_path, dest_path)
      dir_util.copy_tree(src_path, dest_path)
    else:
      print 'Making folder "%s"' % dest_path
      for file_path in STRUCTURE[folder]:
        source_path = SRC + file_path
        if file_path.endswith('/*'):
          print 'Copying files "%s" into folder "%s"' % (source_path, dest_path)
          CopyByRegex(SRC + file_path[:-2], dest_path, '.+')
        else:
          print 'Copying file "%s" into folder "%s"' % (source_path, dest_path)
          shutil.copy(source_path, dest_path)


# Extarcts version number of the release from the sources.
def ExtractVersion():
  global VERSION
  with open(SRC_VERSIONS_FILE) as f:
    content = f.readlines()
  for line in content:
    if line.lstrip().startswith('//'):
      continue
    # Expect: [assembly: AssemblyVersion("X.Y.Z")]
    matches = re.match(r'\[assembly: AssemblyVersion.*\("(\d+)\.(\d+)\.(\d+)(.(\d+))?"\)\]', line)
    if matches:
      VERSION = (int(matches.group(1)),  # MAJOR
                 int(matches.group(2)),  # MINOR
                 int(matches.group(3)),  # PATCH
                 int(matches.group(5) or 0))  # BUILD, optional.
      break
      
  if VERSION is None:
    print 'ERROR: Cannot extract version from: %s' % SRC_VERSIONS_FILE
    exit(-1)
  print 'Releasing version: v%d.%d.%d build %d' % VERSION


# Updates the destination files with the version info.
def PostBuildCopy():
  for source, target in POST_BUILD_COPY:
    print 'Copying "%s" into "%s"...' % (source, target)
    shutil.copy(source, target)


# Updates the source files with the version info.
def UpdateVersionInSources():
  print 'Update repository version file:', SRC_REPOSITORY_VERSION_FILE
  UpdateVersionInJsonFile_(SRC_REPOSITORY_VERSION_FILE)


def UpdateVersionInJsonFile_(name):
  with open(name) as fp:
    content = json.load(fp);
  if not 'VERSION' in content:
    print 'ERROR: Cannot find VERSION in:', name
    exit(-1)
  content['VERSION']['MAJOR'] = VERSION[0]
  content['VERSION']['MINOR'] = VERSION[1]
  content['VERSION']['PATCH'] = VERSION[2]
  content['VERSION']['BUILD'] = VERSION[3]
  with open(name, 'w') as fp:
    json.dump(content, fp, indent=4, sort_keys=True)


def MakeReleaseFileName():
  if VERSION[3]:
    return DEST_RELEASE_NAME_WITH_BUILD_FMT % VERSION
  else:
    return DEST_RELEASE_NAME_FMT % VERSION[:3]


# Creates a package for re-destribution.
def MakePackage():
  if not MAKE_PACKAGE:
    print 'No package requested, skipping.'
    return

  release_name = MakeReleaseFileName();
  package_file_name = '%s/%s.zip' % (DEST_RELEASES, release_name)
  if os.path.exists(package_file_name):
    if not OVERWRITE_PACKAGE:
      print 'ERROR: Package for this version already exists.'
      exit(-1)

    print 'WARNING: Package already exists. Deleting.'
    os.remove(package_file_name)

  print 'Making %s package...' % PACKAGE_TITLE
  code = subprocess.call([
      SHELL_ZIP_BINARY,
      'a',
      package_file_name,
      DEST + '/*'])
  if code != 0:
    print 'ERROR: Failed to make the package.'
    exit(code)


def main(argv):
  global MAKE_PACKAGE, OVERWRITE_PACKAGE, VERSION

  try:
    opts, _ = getopt.getopt(argv[1:], 'po', )
  except getopt.GetoptError:
    print 'make_release.py [-po]'
    exit(2)
  opts = dict(opts)
  MAKE_PACKAGE = '-p' in opts
  OVERWRITE_PACKAGE = '-o' in opts

  ExtractVersion()
  CompileBinary()
  CleanupReleaseFolder()
  UpdateVersionInSources()
  MakeFoldersStructure()
  PostBuildCopy()
  MakePackage()
  print 'SUCCESS!'

main(sys.argv)
