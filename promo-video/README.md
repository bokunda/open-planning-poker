# Open Planning Poker — promo videos

Promo videos built with [Remotion](https://remotion.dev) from real sessions recorded with Playwright on app.openplanningpoker.com.

| File | Format | Length |
|------|--------|--------|
| `out/opp-promo-youtube.mp4` (+ `out/opp-promo-youtube.en.srt`) | 1920×1080, 16:9 | 75 s |
| `out/opp-promo-reels.mp4` | 1080×1920, 9:16 (Instagram Reels / TikTok), burned-in captions | 24 s |
| `out/opp-promo-integrations-youtube.mp4` (+ `.en.srt`) | 1920×1080, 16:9, with Jira, Slack and Teams | 98 s |
| `out/opp-promo-integrations-reels.mp4` | 1080×1920, 9:16, with Jira, Slack and Teams, burned-in captions | 34 s |

In the integrations videos our app is real footage. Slack, Jira and the Teams meeting window are simplified drawings
(`src/mocks.tsx`) with the texts the integrations really post; they are not recordings of those products.

## Pipeline

1. `node capture/record.mjs` — records the host (desktop) and Ana (iPhone) into `public/rec/*.webm` and writes event times to `public/rec/marks.json`. Creates one demo game on production; uses the installed Edge.
   `node capture/record-integrations.mjs` — records the Teams tab: the setup page (with a stub of the Teams SDK), the host in
   a narrow side panel and Ana on the meeting stage, into `public/rec/teams-*.webm` and `teams-marks.json`. Creates one demo game.
2. Narration: `capture/narration.json` → `public/vo/*.mp3` (Microsoft Edge neural voice `en-US-AndrewNeural` via `edge-tts`), durations in `public/vo/durations.json`: `python capture/tts.py [ids]` (needs `edge-tts`, `certifi`, `mutagen`; with no ids it makes only the missing lines).
3. Music bed: `python capture/music.py 100` → `public/music.wav` (synthesised, royalty-free).
4. `npx remotion studio` to preview, `npm run render` renders the first two videos, `npm run render:integrations` the integrations videos and their `.srt`.

Scene timing follows the narration length; clip start times come from `marks.json`, so after a new recording the videos re-sync on their own.

On this machine Node needs `NODE_USE_SYSTEM_CA=0` for HTTPS (npm, Remotion browser download).
