"""Read the local Wwise bank, decode selected hacking sounds, and stage Unity WAV inputs.
Requires the standalone vgmstream-cli decoder (https://github.com/vgmstream/vgmstream).
No EVE files are modified. No decoder binaries are included in the mod package.
"""
import argparse, hashlib, json, struct, subprocess, wave
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument('--eve', type=Path, default=Path('E:/Eve'))
parser.add_argument('--sdk', type=Path, default=Path('F:/SPT 4.1.x/Development/CJ-SDK'))
parser.add_argument('--decoder', type=Path, required=True)
parser.add_argument('--output', type=Path, default=Path('artifacts/eve-reference/audio'))
args = parser.parse_args()
def index(version):
    return {parts[0]: parts[1] for line in (args.eve/version/'resfileindex.txt').read_text().splitlines() if len(parts := line.split(',')) > 1}
tq = index('tq'); metadata = None
bank_id = 'res:/audio/essential_media/interface.bnk'
for version in ('tq', 'sisi'):
    catalog = index(version)
    path = args.eve/'ResFiles'/catalog['res:/audio/soundbanksinfo.json']
    if path.exists() and catalog[bank_id] == tq[bank_id]:
        metadata = json.loads(path.read_text(encoding='utf-8-sig')); break
if metadata is None: raise RuntimeError('No local metadata matching the installed Interface bank.')
bank = next(b for b in metadata['SoundBanksInfo']['SoundBanks'] if b['ShortName'] == 'Interface')
bank_path = args.eve/'ResFiles'/tq[bank_id]; raw = bank_path.read_bytes()
chunks = {}; position = 0
while position + 8 <= len(raw):
    kind = raw[position:position+4].decode('ascii'); size = struct.unpack_from('<I', raw, position+4)[0]
    chunks[kind] = raw[position+8:position+8+size]; position += 8+size
lookup = {identity:(offset,size) for identity,offset,size in struct.iter_unpack('<III', chunks['DIDX'])}
args.output.mkdir(exist_ok=True, parents=True)
receipt = []
for entry in bank['Media']:
    stem = Path(entry['ShortName']).stem
    if not (stem.lower().startswith('hack_') or stem == 'coheren_low_hack_loop'): continue
    source = args.output/(stem+'.wem')
    if entry['Streaming'] == 'true':
        relative = tq.get('res:/audio/media/' + entry['Id'] + '.wem')
        if not relative or not (args.eve/'ResFiles'/relative).exists(): raise RuntimeError('Missing full streamed sound: '+stem)
        source.write_bytes((args.eve/'ResFiles'/relative).read_bytes())
    else:
        if int(entry['Id']) not in lookup: continue
        offset,size = lookup[int(entry['Id'])]; source.write_bytes(chunks['DATA'][offset:offset+size])
    target = args.output/(stem+'.wav')
    subprocess.run([str(args.decoder.resolve()), '-i', '-o', str(target), str(source)], check=True, stdout=subprocess.DEVNULL)
    with wave.open(str(target), 'rb') as wav:
        duration = wav.getnframes()/wav.getframerate()
        if not wav.getnframes() or wav.getframerate() < 8000: raise RuntimeError('Invalid decoded sound: '+stem)
        receipt.append(dict(name=stem, mediaId=entry['Id'], seconds=round(duration,3), channels=wav.getnchannels(), sampleRate=wav.getframerate(), sha256=hashlib.sha256(target.read_bytes()).hexdigest()))
mapping = {'startup':'hack_startup1', 'reveal':'hack_click1', 'attack':'hack_click5', 'cache':'hack_click6',
           'utility':'hack_click4', 'shield-on':'hack_shieldTurnOn', 'shield-off':'hack_shieldOFF',
           'success':'hack_Win2', 'failure':'hack_Loose', 'abort':'hack_shutdown1', 'ambient':'hack_bgloop1', 'low-coherence':'coheren_low_hack_loop'}
destination = args.sdk/'Assets/Mods/SkillsExtended.Assets/Inputs'
if not destination.exists(): raise RuntimeError('Run Stage-Assets.ps1 first.')
for name,source in mapping.items(): (destination/(name+'.wav')).write_bytes((args.output/(source+'.wav')).read_bytes())
manifest = dict(bank=str(bank_path), bankSha256=hashlib.sha256(raw).hexdigest(), actionMapping=mapping, decoded=receipt)
(args.output/'manifest.json').write_text(json.dumps(manifest,indent=2))
(destination/'eve-audio-receipt.json').write_text(json.dumps(manifest,indent=2))
print(f'Decoded {len(receipt)} EVE hacking clips; staged {len(mapping)} WAV cues for Unity.')
