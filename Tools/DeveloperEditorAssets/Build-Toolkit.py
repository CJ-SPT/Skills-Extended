"""Port the self-contained Campaigns Toolkit assets without starting Unity.

Use --source to select a validated Campaigns editor bundle. Both its bundle name
and serialized-file identities are changed so both mods can load concurrently.
Requires UnityPy 1.25.3 (the existing Signals asset tooling dependency).
"""
import argparse
import hashlib
import json
from pathlib import Path
import UnityPy

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Client/SkillsExtended.Client.Skills/Resources/bundles/skills_editor_toolkit.bundle'

def build(source):
    env = UnityPy.load(str(source))
    bundle = next(iter(env.files.values()))
    names = {name: 'CAB-' + hashlib.sha256(('SkillsExtended.DeveloperEditor.' + name.removesuffix('.resS')).encode()).hexdigest()[:32]
             + ('.resS' if name.endswith('.resS') else '') for name in bundle.files}
    def replace(value):
        if isinstance(value, str):
            if value == 'wtt_campaigns_editor_toolkit.bundle':
                return 'skills_editor_toolkit.bundle'
            for old, new in sorted(names.items(), key=lambda pair: -len(pair[0])):
                value = value.replace(old, new)
            return value
        if isinstance(value, list): return [replace(x) for x in value]
        if isinstance(value, dict): return {key: replace(x) for key, x in value.items()}
        return value
    for obj in env.objects:
        tree = obj.read_typetree()
        updated = replace(tree)
        if tree != updated: obj.save_typetree(updated)
    bundle.files = {names[name]: value for name, value in bundle.files.items()}
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    OUTPUT.write_bytes(bundle.save(packer='lz4'))
    verify(OUTPUT)
    receipt = {
        'origin': 'WTT-Campaigns Unity UI Toolkit editor assets (local author-owned source)',
        'source_sha256': hashlib.sha256(source.read_bytes()).hexdigest(),
        'output_sha256': hashlib.sha256(OUTPUT.read_bytes()).hexdigest(),
        'bundle': OUTPUT.name,
        'note': 'Independent bundle/file identities; no Campaigns DLL dependency. Runtime uses the shared theme, panel, font and control templates.',
    }
    (OUTPUT.parents[1] / 'Notices/DeveloperEditorToolkit-Source.json').write_text(json.dumps(receipt, indent=2), encoding='utf-8')
    print(json.dumps(receipt, indent=2))

def verify(path):
    env = UnityPy.load(str(path))
    trees = [o.read_typetree() for o in env.objects]
    assets = next(t for o, t in zip(env.objects, trees) if o.type.name == 'AssetBundle')
    assert assets['m_Name'] == 'skills_editor_toolkit.bundle'
    assert not any('wtt_campaigns_editor_toolkit.bundle' in str(t) for t in trees)
    container = dict(assets['m_Container'])
    for name in ('editorpanel.asset', 'editor.uxml', 'editor.uss', 'field.uxml', 'action.uxml', 'window.uxml',
                 'choicefield.uxml', 'choicepopup.uxml', 'choiceoption.uxml'):
        assert 'assets/mods/wtt-campaigns.assets/editortoolkit/' + name in container, name
    assert 'assets/mods/wtt-campaigns.assets/fonts/bender.ttf' in container
    serialized_controls = str(trees)
    for name in ('editor-scrollbar', 'editor-scroll-slider', 'editor-choice-field', 'editor-choice-panel',
                 'editor-choice-list', 'editor-popup-shield', 'ChoicePanel', 'ChoiceSearch', 'Caption', 'Value'):
        assert name in serialized_controls, 'Missing bundled runtime choice/scroll control: ' + name
    assert len([o for o in env.objects if o.type.name == 'Shader']) >= 3
    assert not any(t.get('m_AssemblyName', '').startswith('WTT') for t in trees), 'Campaigns script dependency'
    serialized = [f for f in next(iter(env.files.values())).files.values() if hasattr(f, 'objects')]
    assert all(x.path == 'Library/unity default resources' for f in serialized for x in f.externals), 'Unexpected external bundle dependency'
    print('Standalone Toolkit bundle verified: theme, UXML, panel, font and embedded shaders.')

if __name__ == '__main__':
    p = argparse.ArgumentParser()
    p.add_argument('--source', type=Path)
    p.add_argument('--verify', action='store_true')
    args = p.parse_args()
    if args.verify: verify(OUTPUT)
    elif args.source: build(args.source)
    else: p.error('Use --source <validated Campaigns bundle>, or --verify.')
