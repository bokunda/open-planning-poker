"""Narration: capture/narration.json -> public/vo/<id>.mp3 and public/vo/durations.json.
Usage: python capture/tts.py [id ...]   (no ids: only lines without an mp3 yet). Needs edge-tts, certifi, mutagen."""
import asyncio, json, ssl, sys, pathlib
import certifi
# Python's Windows certificate store loader fails on this machine; use certifi's bundle instead.
ssl.SSLContext.load_default_certs = lambda self, purpose=ssl.Purpose.SERVER_AUTH: self.load_verify_locations(cafile=certifi.where())
import edge_tts
from mutagen.mp3 import MP3

VOICE = 'en-US-AndrewNeural'
root = pathlib.Path(__file__).resolve().parent.parent
lines = json.loads((root / 'capture/narration.json').read_text(encoding='utf8'))
vo = root / 'public/vo'
ids = sys.argv[1:] or [k for k in lines if not (vo / f'{k}.mp3').exists()]

async def main():
    for k in ids:
        out = vo / f'{k}.mp3'
        for attempt in range(6):
            try:
                await edge_tts.Communicate(lines[k], VOICE).save(str(out))
                if out.stat().st_size > 0:
                    break
            except Exception as e:
                print(k, 'retry', type(e).__name__)
            await asyncio.sleep(3)
        else:
            out.unlink(missing_ok=True)
            sys.exit(f'no audio for {k}')
        print(k)
asyncio.run(main())
durations = {p.stem: round(MP3(p).info.length, 3) for p in sorted(vo.glob('*.mp3'))}
(vo / 'durations.json').write_text(json.dumps(durations, indent=1), encoding='utf8')
print(json.dumps({k: durations[k] for k in ids}))
