import React from 'react';
import { AbsoluteFill, Audio, Sequence, interpolate, staticFile, useCurrentFrame, useVideoConfig } from 'remotion';
import {
  Backdrop, BrowserFrame, C, Chip, FONT_BODY, FONT_DISPLAY, FONT_MONO, Headline, HostClip, Logo, M, MOBILE_OFFSET,
  MobileClip, PhoneFrame, SceneFade, VO, Zoom, ease, sec, useEnter, useFonts,
} from './shared';

const H = M.host;
type Scene = { id: string; vo: string; pad: number; render: (len: number) => React.ReactNode };

/** A host clip in a browser frame under a headline. */
const DesktopScene: React.FC<{ title: string; sub?: string; fromMs: number; rate?: number; chips?: { text: string; at: number; icon?: string }[]; zoom?: { from: number; to: number; fx: number; fy: number } }> = ({ title, sub, fromMs, rate = 1, chips = [], zoom }) => {
  const frame = useCurrentFrame();
  const { durationInFrames } = useVideoConfig();
  const enter = useEnter(2, 18);
  const z = zoom ? interpolate(frame, [0, durationInFrames], [zoom.from, zoom.to], { easing: ease, extrapolateRight: 'clamp' }) : 1;
  return (
    <SceneFade>
      <Backdrop />
      <div style={{ position: 'absolute', left: 120, top: 58, right: 120 }}>
        <Headline text={title} sub={sub} size={58} />
      </div>
      <div style={{ position: 'absolute', left: (1920 - 1380) / 2, top: 220, transform: `translateY(${(1 - enter) * 60}px)`, opacity: enter }}>
        <BrowserFrame width={1380}>
          <Zoom zoom={z} fx={zoom?.fx ?? 0.5} fy={zoom?.fy ?? 0.5}><HostClip fromMs={fromMs} rate={rate} /></Zoom>
        </BrowserFrame>
      </div>
      {chips.map((c, i) => (
        <Sequence key={i} from={c.at} layout="none">
          <div style={{ position: 'absolute', right: 70, top: 330 + i * 92 }}><Chip text={c.text} icon={c.icon} size={30} /></div>
        </Sequence>
      ))}
    </SceneFade>
  );
};

/** Desktop and phone side by side, phone kept in sync with the host clock. */
const SplitScene: React.FC<{ title: string; sub?: string; fromMs: number; rate?: number; phoneFromMs: number; phoneRate?: number; zoom?: { from: number; to: number; fx: number; fy: number }; chips?: { text: string; at: number; icon?: string }[] }> = ({ title, sub, fromMs, rate = 1, phoneFromMs, phoneRate = 1, zoom, chips = [] }) => {
  const frame = useCurrentFrame();
  const { durationInFrames } = useVideoConfig();
  const e1 = useEnter(2, 18);
  const e2 = useEnter(8, 18);
  const z = zoom ? interpolate(frame, [0, durationInFrames], [zoom.from, zoom.to], { easing: ease, extrapolateRight: 'clamp' }) : 1;
  return (
    <SceneFade>
      <Backdrop />
      <div style={{ position: 'absolute', left: 100, top: 58, right: 100 }}>
        <Headline text={title} sub={sub} size={58} />
      </div>
      <div style={{ position: 'absolute', left: 90, top: 235, opacity: e1, transform: `translateY(${(1 - e1) * 60}px)` }}>
        <BrowserFrame width={1250}>
          <Zoom zoom={z} fx={zoom?.fx ?? 0.5} fy={zoom?.fy ?? 0.5}><HostClip fromMs={fromMs} rate={rate} /></Zoom>
        </BrowserFrame>
      </div>
      <div style={{ position: 'absolute', left: 1420, top: 190, opacity: e2, transform: `translateY(${(1 - e2) * 80}px) rotate(${(1 - e2) * 6}deg)` }}>
        <PhoneFrame height={850}><MobileClip fromMs={phoneFromMs} rate={phoneRate} /></PhoneFrame>
        <div style={{ textAlign: 'center', marginTop: 10, fontFamily: FONT_BODY, fontWeight: 600, fontSize: 24, color: '#cfc6f5' }}>Ana, on her phone</div>
      </div>
      {chips.map((c, i) => (
        <Sequence key={i} from={c.at} layout="none">
          <div style={{ position: 'absolute', left: 130, top: 990 - i * 0 }}><Chip text={c.text} icon={c.icon} size={30} /></div>
        </Sequence>
      ))}
    </SceneFade>
  );
};

const Intro: React.FC = () => {
  const frame = useCurrentFrame();
  const e = useEnter(4, 12);
  const cards = ['3', '5', '8', '13', '5'];
  return (
    <SceneFade inFrames={1}>
      <Backdrop />
      <AbsoluteFill style={{ alignItems: 'center', justifyContent: 'center' }}>
        <div style={{ display: 'flex', gap: 26, marginBottom: 70 }}>
          {cards.map((v, i) => {
            const s = useEnter(10 + i * 5, 11);
            const flip = interpolate(frame, [60 + i * 6, 75 + i * 6], [0, 180], { extrapolateLeft: 'clamp', extrapolateRight: 'clamp', easing: ease });
            const front = flip > 90;
            return (
              <div key={i} style={{
                width: 120, height: 170, borderRadius: 18, transform: `translateY(${(1 - s) * 120}px) rotateY(${flip}deg) rotate(${(i - 2) * 5}deg)`,
                opacity: s, background: front ? '#fff' : `repeating-linear-gradient(135deg, ${C.primary} 0 14px, #8a66f5 14px 28px)`,
                border: '4px solid rgba(255,255,255,.9)', boxShadow: '0 20px 50px rgba(0,0,0,.35)', display: 'flex', alignItems: 'center', justifyContent: 'center',
              }}>
                {front && <span style={{ transform: 'scaleX(-1)', fontFamily: FONT_DISPLAY, fontWeight: 800, fontSize: 70, color: C.ink }}>{v}</span>}
              </div>
            );
          })}
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: 34, opacity: e, transform: `scale(${0.85 + e * 0.15})` }}>
          <Logo size={130} />
          <div style={{ fontFamily: FONT_DISPLAY, fontWeight: 800, fontSize: 112, color: '#fff', letterSpacing: -3 }}>
            Open Planning <span style={{ color: C.primary300 }}>Poker</span>
          </div>
        </div>
        <div style={{ marginTop: 34 }}><Headline text="Free, open-source estimation for agile teams" size={46} align="center" delay={22} color="#e6ddff" /></div>
      </AbsoluteFill>
    </SceneFade>
  );
};

const Features: React.FC<{ fromMs: number; rate: number }> = ({ fromMs, rate }) => {
  const at = (ms: number) => Math.max(0, sec((ms - fromMs) / 1000 / rate));
  const items = [
    { at: at(H.roomOptions), text: 'Auto-reveal & async voting', icon: '⚙' },
    { at: at(H.roomOptions) + 20, text: 'Export to PDF & CSV', icon: '⬇' },
    { at: at(H.menu), text: 'English · Srpski · Deutsch', icon: '🌐' },
    { at: at(H.dark), text: 'Dark mode', icon: '☾' },
    { at: at(H.team), text: 'Team rooms & velocity chart', icon: '📈' },
  ];
  return (
    <SceneFade>
      <Backdrop />
      <div style={{ position: 'absolute', left: 100, top: 58 }}><Headline text="Everything a planning session needs" size={58} /></div>
      <div style={{ position: 'absolute', left: 90, top: 220 }}>
        <BrowserFrame width={1240}><HostClip fromMs={fromMs} rate={rate} /></BrowserFrame>
      </div>
      {items.map((c, i) => (
        <Sequence key={i} from={c.at} layout="none">
          <div style={{ position: 'absolute', left: 1375, top: 250 + i * 118 }}><Chip text={c.text} icon={c.icon} size={29} /></div>
        </Sequence>
      ))}
    </SceneFade>
  );
};

const Outro: React.FC = () => {
  const e = useEnter(2, 12);
  const e2 = useEnter(14, 14);
  const e3 = useEnter(26, 14);
  return (
    <SceneFade outFrames={20}>
      <Backdrop />
      <AbsoluteFill style={{ alignItems: 'center', justifyContent: 'center' }}>
        <div style={{ opacity: e, transform: `scale(${0.8 + e * 0.2})` }}><Logo size={170} /></div>
        <div style={{ marginTop: 36, fontFamily: FONT_DISPLAY, fontWeight: 800, fontSize: 96, color: '#fff', letterSpacing: -3, opacity: e }}>
          Open Planning <span style={{ color: C.primary300 }}>Poker</span>
        </div>
        <div style={{ marginTop: 18, fontFamily: FONT_BODY, fontWeight: 600, fontSize: 40, color: '#cfc6f5', opacity: e2 }}>
          Free forever · Open source · No sign-up
        </div>
        <div style={{
          marginTop: 56, padding: '26px 56px', borderRadius: 999, background: C.primary, color: '#fff', fontFamily: FONT_MONO, fontWeight: 700, fontSize: 48,
          boxShadow: '0 20px 60px rgba(106,58,232,.6)', opacity: e3, transform: `translateY(${(1 - e3) * 30}px)`,
        }}>app.openplanningpoker.com</div>
        <div style={{ marginTop: 34, fontFamily: FONT_BODY, fontSize: 28, color: '#9d95c9', opacity: e3 }}>github.com/bokunda/open-planning-poker</div>
      </AbsoluteFill>
    </SceneFade>
  );
};

// Scene list: each scene lasts its narration plus a pad. Clip start times come from the recording marks.
const featuresFrom = H.roomOptions - 900;
const featuresRate = (H.end - featuresFrom) / 1000 / (VO.y9 + 0.9);
export const SCENES: Scene[] = [
  { id: 'intro', vo: 'y1', pad: 1.3, render: () => <Intro /> },
  { id: 'create', vo: 'y2', pad: 0.6, render: () => <DesktopScene title="Create a room in seconds" sub="No sign-up. No passwords." fromMs={H.createDialog - 400} rate={1.05} chips={[{ text: 'Fibonacci, T-shirt or custom decks', at: 40, icon: '🃏' }]} /> },
  { id: 'import', vo: 'y3', pad: 0.6, render: () => <DesktopScene title="Paste your backlog" sub="Every line becomes a ticket." fromMs={H.importDialog - 300} rate={2.2} /> },
  { id: 'invite', vo: 'y4', pad: 0.6, render: () => <DesktopScene title="Invite your team" sub="Share the link or scan the QR code." fromMs={H.invite - 500} zoom={{ from: 1, to: 1.25, fx: 0.5, fy: 0.42 }} /> },
  { id: 'join', vo: 'y5', pad: 0.6, render: (len) => <SplitScene title="Join from any device" sub="Laptop or phone, right in the browser." fromMs={H.joinStart} rate={(H.joined + 600 - H.joinStart) / 1000 / (len / 30)} phoneFromMs={1300} /> },
  { id: 'vote', vo: 'y6', pad: 0.6, render: () => <SplitScene title="Vote in secret" sub="See who voted, never what. Nobody anchors the team." fromMs={H.timer - 400} phoneFromMs={H.timer - 400 - MOBILE_OFFSET} chips={[{ text: 'Shared timer for the room', at: 15, icon: '⏱' }]} /> },
  { id: 'reveal', vo: 'y7', pad: 1.0, render: () => <SplitScene title="Flip all cards at once" fromMs={H.reveal - 500} phoneFromMs={H.reveal - 500 - MOBILE_OFFSET} zoom={{ from: 1, to: 1.45, fx: 0.37, fy: 0.55 }} /> },
  { id: 'results', vo: 'y8', pad: 0.6, render: () => <DesktopScene title="Talk about the outliers" sub="Average, median, spread and the final estimate." fromMs={H.results - 400} chips={[{ text: 'Wide spread? Ask who voted 3 and 8', at: 30, icon: '💬' }, { text: 'Record the final estimate', at: sec((H.final - H.results) / 1000), icon: '✔' }]} /> },
  { id: 'features', vo: 'y9', pad: 0.9, render: () => <Features fromMs={featuresFrom} rate={featuresRate} /> },
  { id: 'outro', vo: 'y10', pad: 1.8, render: () => <Outro /> },
];
const VO_DELAY = 8; // frames of silence before each line
export const sceneFrames = (s: Scene) => sec(VO[s.vo] + s.pad) + VO_DELAY;
export const YT_DURATION = SCENES.reduce((a, s) => a + sceneFrames(s), 0);

export const YouTube: React.FC = () => {
  useFonts();
  const frame = useCurrentFrame();
  let start = 0;
  const musicVol = interpolate(frame, [0, 30, YT_DURATION - 60, YT_DURATION], [0, 0.16, 0.16, 0], { extrapolateLeft: 'clamp', extrapolateRight: 'clamp' });
  return (
    <AbsoluteFill style={{ background: C.navy }}>
      <Audio src={staticFile('music.wav')} volume={musicVol} />
      {SCENES.map((s) => {
        const len = sceneFrames(s);
        const from = start;
        start += len;
        return (
          <Sequence key={s.id} from={from} durationInFrames={len} name={s.id}>
            {s.render(len)}
            <Sequence from={VO_DELAY} layout="none"><Audio src={staticFile(`vo/${s.vo}.mp3`)} /></Sequence>
          </Sequence>
        );
      })}
    </AbsoluteFill>
  );
};
