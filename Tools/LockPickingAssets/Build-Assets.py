"""Compile selected purchased FBX meshes into an embedded runtime resource, without an editor.

Reads only the Mini Pack's Unity mesh/texture entries. Source FBX/prefabs are never
copied into the repository or release ZIP. Requires Python 3, no third-party packages.
"""
import argparse
import hashlib
import io
import json
from pathlib import Path
import struct
import zipfile
import zlib


def fbx(data):
    stream = io.BytesIO(data)
    if stream.read(23) != b'Kaydara FBX Binary  \x00\x1a\x00':
        raise ValueError('Expected binary FBX')
    version, = struct.unpack('<I', stream.read(4))
    wide = version >= 7500

    def prop():
        t = stream.read(1).decode()
        if t in 'YCFDIL':
            fmt = {'Y':'h','C':'?','F':'f','D':'d','I':'i','L':'q'}[t]
            return struct.unpack('<'+fmt, stream.read(struct.calcsize(fmt)))[0]
        if t in 'SR':
            n, = struct.unpack('<I', stream.read(4))
            value = stream.read(n)
            return value.decode('utf8') if t == 'S' else value
        if t in 'fdilbc':
            n, encoding, length = struct.unpack('<III', stream.read(12))
            value = stream.read(length)
            if encoding:
                value = zlib.decompress(value)
            fmt = {'f':'f','d':'d','i':'i','l':'q','b':'?','c':'b'}[t]
            return struct.unpack('<'+str(n)+fmt, value)
        raise ValueError('Unsupported FBX property '+t)

    def node():
        end, count, size = struct.unpack('<QQQ' if wide else '<III', stream.read(24 if wide else 12))
        length = stream.read(1)[0]
        if not end:
            return None
        name = stream.read(length).decode()
        values = [prop() for _ in range(count)]
        children = []
        while stream.tell() < end:
            child = node()
            if child is None:
                break
            children.append(child)
        stream.seek(end)
        return name, values, children

    nodes = []
    while stream.tell() < len(data):
        n = node()
        if n is None:
            break
        nodes.append(n)
    return nodes


def child(node, name):
    return next(n for n in node[2] if n[0] == name)


def mesh(data):
    objects = next(n for n in fbx(data) if n[0] == 'Objects')
    geom = next(n for n in objects[2] if n[0] == 'Geometry' and n[1][-1] == 'Mesh')
    vertices = child(geom, 'Vertices')[1][0]
    indices = child(geom, 'PolygonVertexIndex')[1][0]
    uv_layer = child(geom, 'LayerElementUV')
    uvs = child(uv_layer, 'UV')[1][0]
    uv_indices = child(uv_layer, 'UVIndex')[1][0]
    normals = child(geom, 'LayerElementNormal')
    if child(normals, 'MappingInformationType')[1][0] != 'ByPolygonVertex':
        raise ValueError('Unexpected normal mapping')
    normal_data = child(normals, 'Normals')[1][0]
    normal_indices = child(normals, 'NormalsIndex')[1][0] if child(normals, 'ReferenceInformationType')[1][0] == 'IndexToDirect' else range(len(indices))
    output, triangles, polygon = [], [], []
    for corner, raw in enumerate(indices):
        index = -raw-1 if raw < 0 else raw
        uv = uv_indices[corner]
        # FBX authoring centimetres -> metres. Keep source handedness, reverse at runtime.
        v = list(vertices[index*3:index*3+3])
        ni = normal_indices[corner]
        n = list(normal_data[ni*3:ni*3+3])
        output.append([v[0]/100, v[1]/100, v[2]/100, *n, *uvs[uv*2:uv*2+2]])
        polygon.append(corner)
        if raw < 0:
            for j in range(1, len(polygon)-1):
                triangles.extend([polygon[0], polygon[j], polygon[j+1]])
            polygon = []
    if polygon or len(output) == 0:
        raise ValueError('Invalid mesh')
    result = struct.pack('<II', len(output), len(triangles))
    result += b''.join(struct.pack('<8f', *v) for v in output)
    result += struct.pack('<'+str(len(triangles))+'I', *triangles)
    bounds = [[min(v[a] for v in output), max(v[a] for v in output)] for a in range(3)]
    return result, {'vertices':len(output), 'triangles':len(triangles)//3, 'bounds':bounds}


def build(package, output, receipt):
    report = {'author':'Warren Marshall', 'product':'Mini Pack: Lockpick Kit',
              'source':'https://www.artstation.com/marketplace/p/kxl/mini-pack-lockpick-kit',
              'packageSha256':hashlib.sha256(package.read_bytes()).hexdigest(), 'meshes':{}, 'sources':{}}
    compiled = {}
    with zipfile.ZipFile(package) as source:
        for name in ['Housing_01', 'Housing_02', 'Lock_01', 'Lock_02', 'Pick_01', 'Pick_02', 'Pick_03', 'Pick_04', 'Screwdriver']:
            path = 'Unity/MP_Lockpick/Meshes/MESH_'+name+'.fbx'
            data = source.read(path)
            compiled[name+'.mesh'], report['meshes'][name] = mesh(data)
            report['sources'][path] = hashlib.sha256(data).hexdigest()
        for name in ['AT', 'MS', 'N']:
            path = 'Unity/MP_Lockpick/Textures/T_Lockpick_'+name+'.png'
            data = source.read(path)
            compiled[name+'.png'] = data
            report['sources'][path] = hashlib.sha256(data).hexdigest()
    output.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(output, 'w', zipfile.ZIP_DEFLATED) as bundle:
        for name, data in sorted(compiled.items()):
            info = zipfile.ZipInfo(name, (2020,1,1,0,0,0))
            info.compress_type = zipfile.ZIP_DEFLATED
            bundle.writestr(info, data)
    report['outputSha256'] = hashlib.sha256(output.read_bytes()).hexdigest()
    receipt.parent.mkdir(parents=True, exist_ok=True)
    receipt.write_text(json.dumps(report, indent=2)+'\n')
    print(json.dumps(report, indent=2))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('package', type=Path)
    parser.add_argument('--output', type=Path, default=Path('Client/SkillsExtended.Client.Skills/Assets/LockPicking.assets'))
    parser.add_argument('--receipt', type=Path, default=Path('artifacts/lockpicking/source-receipt.json'))
    args = parser.parse_args()
    build(args.package, args.output, args.receipt)
