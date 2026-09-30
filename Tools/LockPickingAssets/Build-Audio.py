"""Build the Tiny metal/lockpicking PCM bank. Requires NumPy and SoundFile.

Input is the original WAV identified by the segment manifest.
The source hash must match Audio-Segments.json; no network access is performed.
"""
import argparse
import hashlib
import io
import json
from pathlib import Path
import wave
import zipfile

import numpy as np
import soundfile as sf

ROOT = Path(__file__).resolve().parents[2]

def pcm(samples, rate):
    output = io.BytesIO()
    with wave.open(output, 'wb') as wav:
        wav.setnchannels(1)
        wav.setsampwidth(2)
        wav.setframerate(rate)
        wav.writeframes(np.rint(samples * 32767).astype('<i2').tobytes())
    return output.getvalue()

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('source', type=Path)
    parser.add_argument('--manifest', type=Path, default=Path(__file__).with_name('Audio-Segments.json'))
    parser.add_argument('--output', type=Path, default=ROOT/'Client/SkillsExtended.Client.Skills/Assets/LockPicking.audio')
    parser.add_argument('--review', type=Path, default=ROOT/'artifacts/lockpicking/audio-review')
    args = parser.parse_args()
    manifest = json.loads(args.manifest.read_text(encoding='utf-8'))
    source_hash = hashlib.sha256(args.source.read_bytes()).hexdigest()
    if source_hash != manifest['source']['sha256']:
        raise ValueError('Source differs from the reviewed manifest; update the provenance and verify segment timing.')
    recording, rate = sf.read(args.source, always_2d=True, dtype='float64')
    if rate != 44100 or recording.shape[1] != 2 or not np.isfinite(recording).all():
        raise ValueError('Expected finite 44.1 kHz stereo source.')
    mono = recording.mean(axis=1)
    args.review.mkdir(parents=True, exist_ok=True)
    clips, report, montage = {}, [], []
    for segment in manifest['segments']:
        name = segment['name']
        start, end = (round(segment[key] * rate) for key in ('start', 'end'))
        if not 0 <= start < end <= len(mono) or name in clips:
            raise ValueError('Invalid or duplicate segment: ' + name)
        samples = mono[start:end].copy()
        samples -= samples.mean()
        peak = np.abs(samples).max()
        gain = min(2.0, segment['peak'] / peak)
        samples *= gain
        attack, release = round(.004 * rate), round(.018 * rate)
        samples[:attack] *= np.linspace(0, 1, attack)
        samples[-release:] *= np.linspace(1, 0, release)
        if np.abs(samples).max() > .6 or np.sqrt(np.mean(samples**2)) < .001:
            raise ValueError('Clipped or silent segment: ' + name)
        data = pcm(samples, rate)
        clips[name] = data
        (args.review/(name+'.wav')).write_bytes(data)
        montage.extend((samples, np.zeros(round(.35 * rate))))
        report.append(dict(name=name, start=segment['start'], end=segment['end'],
                           duration=len(samples)/rate, peak=float(np.abs(samples).max()),
                           rms=float(np.sqrt(np.mean(samples**2))), gain=float(gain),
                           sha256=hashlib.sha256(data).hexdigest()))
    args.output.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(args.output, 'w', zipfile.ZIP_DEFLATED) as archive:
        for name, data in sorted(clips.items()):
            info = zipfile.ZipInfo(name+'.wav', (2020, 1, 1, 0, 0, 0))
            info.compress_type = zipfile.ZIP_DEFLATED
            archive.writestr(info, data)
    (args.review/'Tiny-metal-audition.wav').write_bytes(pcm(np.concatenate(montage), rate))
    receipt = dict(source=manifest['source'], output_sha256=hashlib.sha256(args.output.read_bytes()).hexdigest(), clips=report)
    (args.review/'receipt.json').write_text(json.dumps(receipt, indent=2)+'\n', encoding='utf-8')
    print(json.dumps(dict(clips=len(clips), sha256=receipt['output_sha256'], review=str(args.review))))

if __name__ == '__main__':
    main()
