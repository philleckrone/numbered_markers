# -*- coding: utf-8 -*-
# Writes the Numbered Markers user guides (English, Spanish and French) from rtf_text.py:
#     python tools/make_rtf.py
# Lines starting with '# ' are headings; '> ' lines are indented.
import io, os, re, sys

def esc(s):
    out = []
    for ch in s:
        o = ord(ch)
        if ch in u'\\{}':
            out.append(u'\\' + ch)
        elif o < 128:
            out.append(ch)
        elif o < 256:
            out.append(u"\\'%02x" % o)
        else:
            out.append(u'\\u%d?' % (o if o < 32768 else o - 65536))
    return u''.join(out)

def rtf(title, lines):
    r = [u'{\\rtf1\\ansi\\ansicpg1252\\deff0\\nouicompat{\\fonttbl{\\f0\\fswiss\\fcharset0 Arial;}}',
         u'\\viewkind4\\uc1',
         u'\\pard\\sa200\\sl276\\slmult1\\f0\\fs28\\b ' + esc(title) + u'\\b0\\fs22\\par']
    for ln in lines:
        if ln.startswith(u'# '):
            r.append(u'\\par')
            r.append(u'\\b ' + esc(ln[2:]) + u'\\b0\\par')
        elif ln.startswith(u'> '):
            r.append(u'\\tab ' + esc(ln[2:]) + u'\\par')
        else:
            r.append(esc(ln) + u'\\par')
    r.append(u'}')
    return u'\r\n'.join(r) + u'\r\n'

from rtf_text import EN, ES, FR

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
# The version is set once, in Strings.ToolVersion.
VERSION = re.search(r'ToolVersion = "([^"]+)"', io.open(os.path.join(ROOT, 'NumberedMarkersPlugin', 'Strings.cs'), encoding='ascii').read()).group(1)
io.open(os.path.join(ROOT, 'NumberedMarkers_UserGuide.rtf'), 'w', encoding='ascii', newline='').write(rtf(u'NUMBERED MARKERS %s (PARATEXT PLUGIN)' % VERSION, EN))
io.open(os.path.join(ROOT, 'NumberedMarkers_UserGuide_sp.rtf'), 'w', encoding='ascii', newline='').write(rtf(u'MARCADORES NUMERADOS %s (COMPLEMENTO DE PARATEXT)' % VERSION, ES))
io.open(os.path.join(ROOT, 'NumberedMarkers_UserGuide_fr.rtf'), 'w', encoding='ascii', newline='').write(rtf(u'MARQUEURS NUMÉROTÉS %s (MODULE PARATEXT)' % VERSION, FR))
print('ok')
