// Writes the .srt of a 16:9 video from its scene timing: `node capture/srt.mjs` for the integrations video,
// `node capture/srt.mjs tour` for the feature tour.
import { build } from 'esbuild';
import fs from 'node:fs';

const out = 'capture/tmp-srt.mjs';
const tour = process.argv[2] === 'tour';
await build({ entryPoints: [tour ? 'src/Tour.tsx' : 'src/YouTubeIntegrations.tsx'], bundle: true, format: 'esm', platform: 'node', outfile: out, jsx: 'automatic', loader: { '.json': 'json' }, logLevel: 'error' });
const mod = await import(`./tmp-srt.mjs?${Date.now()}`);
const narrationTimesI = tour ? mod.narrationTimesTour : mod.narrationTimesI;
fs.rmSync(out);
const text = JSON.parse(fs.readFileSync('capture/narration.json', 'utf8'));
const ts = (s) => {
  const ms = Math.round(s * 1000);
  const p = (n, w = 2) => String(n).padStart(w, '0');
  return `${p(Math.floor(ms / 3600000))}:${p(Math.floor(ms / 60000) % 60)}:${p(Math.floor(ms / 1000) % 60)},${p(ms % 1000, 3)}`;
};
const srt = narrationTimesI().map((n, i) => `${i + 1}\n${ts(n.start)} --> ${ts(n.end)}\n${text[n.vo].replace('slash poker', '/poker').replace('app dot open planning poker dot com', 'app.openplanningpoker.com')}\n`).join('\n');
fs.mkdirSync('out', { recursive: true });
fs.writeFileSync(tour ? 'out/opp-tour-youtube.en.srt' : 'out/opp-promo-integrations-youtube.en.srt', srt);
console.log(srt.split('\n').slice(0, 8).join('\n'));
