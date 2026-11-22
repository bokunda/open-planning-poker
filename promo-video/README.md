# Open Planning Poker — promo videos

Promo videos built with [Remotion](https://remotion.dev) from real sessions recorded with Playwright on app.openplanningpoker.com.

| File | Format | Length |
|------|--------|--------|
| `out/opp-promo-youtube.mp4` (+ `out/opp-promo-youtube.en.srt`) | 1920×1080, 16:9 | 75 s |
| `out/opp-promo-reels.mp4` | 1080×1920, 9:16 (Instagram Reels / TikTok), burned-in captions | 24 s |
| `out/opp-promo-integrations-youtube.mp4` (+ `.en.srt`) | 1920×1080, 16:9, with Jira, Slack and Teams | 98 s |
| `out/opp-promo-integrations-reels.mp4` | 1080×1920, 9:16, with Jira, Slack and Teams, burned-in captions | 34 s |
| `out/opp-tour-youtube.mp4` (+ `out/opp-tour-youtube.en.srt`) | 1920×1080, 16:9, the full feature tour of v3.8.0 | 177 s |
| `out/opp-tour-tiktok.mp4` | 1080×1920, 9:16 (TikTok / Reels / Shorts), the feature tour, burned-in captions | 42 s |

In the integrations videos our app is real footage. Slack, Jira and the Teams meeting window are simplified drawings
(`src/mocks.tsx`) with the texts the integrations really post; they are not recordings of those products.

## Pipeline

1. `node capture/record.mjs` — records the host (desktop) and Ana (iPhone) into `public/rec/*.webm` and writes event times to `public/rec/marks.json`. Creates one demo game on production; uses the installed Edge.
   `node capture/record-integrations.mjs` — records the Teams tab: the setup page (with a stub of the Teams SDK), the host in
   a narrow side panel and Ana on the meeting stage, into `public/rec/teams-*.webm` and `teams-marks.json`. Creates one demo game.
   For the feature tour: `node capture/seed-run.mjs` creates anonymous demo users, two team rooms with past sprints and
   an organization on production (ids and tokens in the gitignored `capture/tmp-seed/`), then
   `node capture/record-tour.mjs [team game retro org api]` records one video per segment into `public/rec/tour-*.webm`
   with `tour-marks.json`. Only the host is recorded; the other players act through the GraphQL API as themselves. A retro
   can be started once per sprint, so re-record that segment with `RETRO_SPRINT="Sprint 22"`. Afterwards
   `node capture/revoke-tokens.mjs` revokes the API token that shows in the video.
2. Narration: `capture/narration.json` → `public/vo/*.mp3` (Microsoft Edge neural voice `en-US-AndrewNeural` via `edge-tts`), durations in `public/vo/durations.json`: `python capture/tts.py [ids]` (needs `edge-tts`, `certifi`, `mutagen`; with no ids it makes only the missing lines).
3. Music bed: `python capture/music.py 100` → `public/music.wav` (synthesised, royalty-free).
4. `npx remotion studio` to preview, `npm run render` renders the first two videos, `npm run render:integrations` the integrations videos and their `.srt`, `npm run render:tour` the feature tour and its `.srt`.

Scene timing follows the narration length; clip start times come from `marks.json`, so after a new recording the videos re-sync on their own.

On this machine Node needs `NODE_USE_SYSTEM_CA=0` for HTTPS (npm, Remotion browser download).
