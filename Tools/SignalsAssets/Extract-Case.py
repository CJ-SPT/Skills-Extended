"""Extract one dependency-complete native case from a locally exported native library.
No game/editor process is used. Source assets remain unchanged.
Requires UnityPy 1.25.3. The resulting bundle has its own CAB identity.
"""
import argparse, hashlib, json
from pathlib import Path
import UnityPy

class Stream:
    flags = 0
    def __init__(self, data): self.data = data
    def save(self): return self.data

def build(source, destination):
    env = UnityPy.load(str(source))
    bundle = next(iter(env.files.values()))
    asset = next(v for v in bundle.files.values() if hasattr(v, 'objects'))
    header = next(o for o in asset.objects.values() if o.type.name == 'AssetBundle')
    tree = header.read_typetree()
    name = 'containers/5909d50c86f774659e6aaebe.prefab'
    entry = next(v for k, v in tree['m_Container'] if k == name)
    refs = tree['m_PreloadTable'][entry['preloadIndex']:entry['preloadIndex']+entry['preloadSize']]
    keep = {v['m_PathID'] for v in refs} | {header.path_id}
    asset.objects = {k:v for k,v in asset.objects.items() if k in keep}
    cab = 'CAB-skills-extended-signal-case'
    stream = bytearray()
    def relocate(node):
        if isinstance(node, dict):
            if 'offset' in node and 'size' in node and 'path' in node and node['size']:
                resource = next(v for k,v in bundle.files.items() if k.endswith(Path(node['path']).name))
                resource.Position = node['offset']
                data = resource.read_bytes(node['size'])
                node['offset'] = len(stream); node['path'] = 'archive:/'+cab+'/'+cab+'.resS'
                stream.extend(data)
            for v in node.values(): relocate(v)
        elif isinstance(node, list):
            for v in node: relocate(v)
    for obj in asset.objects.values():
        if obj == header: continue
        data = obj.read_typetree(); relocate(data); obj.save_typetree(data)
    entry['preloadIndex'] = 0
    tree['m_PreloadTable'] = refs
    tree['m_Container'] = [('signal-case.prefab', entry)]
    tree['m_Name'] = tree['m_AssetBundleName'] = 'skills-signal-case'
    header.save_typetree(tree)
    asset.name = cab
    bundle.files = {cab:asset, cab+'.resS':Stream(bytes(stream))}
    raw = bundle.save(packer='lz4')
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_bytes(raw)
    check = UnityPy.load(str(destination))
    objects = {o.path_id for o in check.objects}
    def validate(node):
        if isinstance(node, dict):
            if 'm_FileID' in node and 'm_PathID' in node:
                assert node['m_FileID'] == 0
                assert node['m_PathID'] == 0 or node['m_PathID'] in objects
            else:
                for v in node.values(): validate(v)
        elif isinstance(node, (list, tuple)):
            for v in node: validate(v)
    for obj in check.objects: validate(obj.read_typetree())
    assert list(check.container) == ['signal-case.prefab']
    print(json.dumps({'bytes':len(raw),'objects':len(objects),'sha256':hashlib.sha256(raw).hexdigest()}))

if __name__ == '__main__':
    p=argparse.ArgumentParser(); p.add_argument('source',type=Path); p.add_argument('destination',type=Path)
    a=p.parse_args(); build(a.source,a.destination)
