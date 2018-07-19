"""A client linrary to communicate with CurseForge via API.

Example:
  import CurseForgeClient

  CurseForgeClient.CURSE_PROJECT_ID = '123456'
  CurseForgeClient.CURSE_API_TOKEN = '11111111-2222-3333-4444-555555555555'
  print 'KSP 1.4.*:', CurseForgeClient.GetVersions(r'1\.4\.\d+')
  CurseForgeClient.UploadFile(
      '/var/files/archive.zip', '# BLAH!', r'1\.4\.\d+')
"""
import json
import re
import urllib2
import FormDataUtil


# The token to use when accessing CurseForge. NEVER commit it to GitHub!
# The caller code must set this variable before using the client.
CURSE_API_TOKEN = None

# The CurseForge project to work with. It must be set before using the client.
CURSE_PROJECT_ID = None

# This binds this client to the KSP namespace.
CURSE_BASE_URL = 'https://kerbal.curseforge.com'

# The actions paths.
CURSE_UPLOAD_URL_TMPL = '/api/projects/{project}/upload-file'
CURSE_API_GET_VERSIONS = '/api/game/versions'

# The latest versions for the game.
cached_versions = None


def GetAuthorizedEndpoint(api_path, headers=None):
  """Gets API URL and the authorization headers.

  The authorization token must be set in the global variable CURSE_API_TOKEN.
  Otherwise, the endpoint will try to access the function anonymously.
  """
  url = CURSE_BASE_URL + api_path
  if not headers:
    headers = {}
  if CURSE_API_TOKEN:
    headers['X-Api-Token'] = CURSE_API_TOKEN
  return url, headers
  

def GetKSPVersions(pattern=None):
  """Gets the available versions for the game.

  This method caches the versions fetched from the server. It's OK to call it
  multiple times, it will only request the server once.

  Note, that the versions call requires an authorization token.
  See {@GetAuthorizedEndpoint}.

  Args:
    pattern: A regexp string to apply on the result.
  Returns:
    A list of objects: { 'name': <KSP name>, 'id': <CurseForge ID> }. The list
    will be filtered if the pattern is set.
  """
  global cached_versions
  if not cached_versions:
    print 'Requesting versions for:', CURSE_BASE_URL
    url, headers = GetAuthorizedEndpoint(CURSE_API_GET_VERSIONS);
    json_response = json.loads(_CallAPI(url, None, headers))
    cached_versions = map(lambda x: {'name': x['name'], 'id': x['id']}, json_response)
  if pattern:
    regex = re.compile(pattern)
    return filter(lambda x: regex.match(x['name']), cached_versions)
  return cached_versions


def UploadFileEx(metadata, filepath):
  """Uploads the file to the CurseForce project given the full metadata.

  Args:
    metadata: See https://authors.curseforge.com/docs/api for details.
    filepath: A full or relative path to the local file.
  Returns:
    The response object, returned by the API.
  """
  headers, data = FormDataUtil.EncodeFormData([
      { 'name': 'metadata', 'data': metadata },
      { 'name': 'file', 'filename': filepath },
  ])
  url, headers = GetAuthorizedEndpoint(
      CURSE_UPLOAD_URL_TMPL.format(project=PROJECT_ID), headers)
  return json.loads(_CallAPI(url, data, headers))


def UploadFile(filepath, changelog, versions_pattern,
               title=None, release_type='release',
               changelog_type='markdown'):
  """Uploads the file to the CurseForge project.

  Args:
    filepath: A full or relative path to the local file.
    changelog: The change log content.
    versions_pattern: The RegExp string to find the target versions.
    title: The user friendly title of the file. If not porvied, the file name
        is used.
    release_type: The type of the release. Allowed values: release, alpha, beta.
    changelog_type: The formatting type of the changelog.
  Returns:
    The response object, returned by the API.
  """
  metadata = {
    'changelog': changelog,
    'changelogType': changelog_type,
    'displayName': title,
    'gameVersions': map(lambda x: x['id'], GetKSPVersions(versions_pattern)),
    'releaseType': release_type,
  }
  return UploadFileEx(metadata, filepath)


def _CallAPI(url, data, headers):
  """Invokes the API call. raises in case of any error."""
  try:
    request = urllib2.Request(url, data, headers)
    response = urllib2.urlopen(request)
  except urllib2.HTTPError as ex:
    error_message = ex.read()
    print 'API call failed:', error_message
    raise ex
  return response.read()
