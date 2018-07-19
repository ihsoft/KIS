"""Provids helpers to deal with the multipart/form-data MIME types."""
import io
import json
import random
import string
import ntpath


def IdGenerator(size, chars=string.ascii_letters + string.digits):
  """Makes a unique random string of the requested size."""
  return ''.join(random.choice(chars) for _ in range(size))


def EncodeFormData(fields):
  """Encodes the provided arguments as the web form fields.

  The argument must be an array of objects that define the fields to be passed:
  - 'name': A required property than defies the name of the web form field.
  - 'data': The raw data to pass. If it's of type 'string', then it's passed as
    'text/plain'. Otherwise, it's serilazlied as JSON and passed as
    'application/json'.
  - 'filename': An optional property that designates a path to the binary file
    to tanrsfer. The 'data' field is ignored in this case, and the real data is
    read from the file. The data is passed as 'application/octet-stream', and
    server will receive the file name part of the path. The part can be
    relative, in which case it's counted realtive to the main module, or it can
    be absolute.

  Example:
    [
        { 'name': 'metadata', 'data': metadatObj },
        { 'name': 'file', 'filename': '/home/me/releases/MyMod_v1.0.zip' }
    ]

  In case of encoding files, an IO expcetion may be raised.

  @param fields: An array of the fields to encode.
  """
  boundry = '----WebKitFormBoundary' + IdGenerator(16)

  data_stream = io.BytesIO()
  for field in fields:
    filename = field.get('filename')
    if filename:
      with open(filename, 'rb') as f:
        data = f.read()
      content_type = 'application/octet-stream'
      filename = ntpath.basename(filename)
    else:
      data = field['data']
      if type(data) is not str:
        content_type = 'application/json'
        data  = json.dumps(data)
      else:
        content_type = 'text/plain'
    _WriteFormData(data_stream, boundry,
                   field['name'], content_type, data, filename)

  data_stream.write(b'--')
  data_stream.write(boundry.encode())
  data_stream.write(b'--')
  data_stream.write(b'\r\n')

  headers = {
      'Content-Type': 'multipart/form-data; boundary=%s' % boundry,
      'Content-Length': str(data_stream.tell()),
  }
  return headers, data_stream.getvalue()


def _WriteFormData(stream, boundry, name, content_type, data, filename=None):
  """Helper method to write a web form field."""
  stream.write(b'--')
  stream.write(boundry.encode())
  stream.write(b'\r\n')

  if filename:
    stream.write(('Content-Disposition: form-data; name="%s"; filename="%s"' %
                  (name, filename)).encode())
  else:
    stream.write(('Content-Disposition: form-data; name="%s"' %
                  (name)).encode())
  stream.write(b'\r\n')
  stream.write(('Content-Type: %s' % content_type).encode())
  stream.write(b'\r\n')
  stream.write(b'\r\n')
  stream.write(data)
  stream.write(b'\r\n')
