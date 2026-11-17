# Open Planning Poker — promo videos

Two videos built with [Remotion](https://remotion.dev) from a real session recorded with Playwright on app.openplanningpoker.com.

| File | Format | Length |
|------|--------|--------|
| `out/opp-promo-youtube.mp4` (+ `out/opp-promo-youtube.en.srt`) | 1920×1080, 16:9 | 75 s |
| `out/opp-promo-reels.mp4` | 1080×1920, 9:16 (Instagram Reels / TikTok), burned-in captions | 24 s |

## Pipeline

1. `node capture/record.mjs` — records the host (desktop) and Ana (iPhone) into `public/rec/*.webm` and writes event times to `public/rec/marks.json`. Creates one demo game on production; uses the installed Edge.
2. Narration: `capture/narration.json` → `public/vo/*.mp3` (Microsoft Edge neural voice `en-US-AndrewNeural` via `edge-tts`), durations in `public/vo/durations.json`.
3. Music bed: `python capture/music.py 100` → `public/music.wav` (synthesised, royalty-free).
4. `npx remotion studio` to preview, `npm run render` to render both videos to `out/`.

Scene timing follows the narration length; clip start times come from `marks.json`, so after a new recording the videos re-sync on their own.

On this machine Node needs `NODE_USE_SYSTEM_CA=0` for HTTPS (npm, Remotion browser download).
