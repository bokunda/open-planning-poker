import React from 'react';
import { AbsoluteFill, Audio, Sequence, interpolate, staticFile, useCurrentFrame, useVideoConfig } from 'remotion';
import {
  Backdrop, BrowserFrame, C, FONT_BODY, FONT_DISPLAY, FONT_MONO, HostClip, Logo, M, MobileClip, PhoneFrame,
  SceneFade, Subtitle, VO, Zoom, ease, sec, useEnter, useFonts,
} from './shared';
import narration from '../capture/narration.json';

const H = M.host;
const MO = M.mobile;
const TEXT = narration as Record<string, string>;
type Scene = { id: string; vo: string; pad: number; render: () => React.ReactNode };

export const Title: React.FC<{ text: string; accent?: string; top?: number; size?: number }> = ({ text, accent, top = 170, size = 92 }) => {
  const e = useEnter(0, 14);
  return (
    <div style={{ position: 'absolute', top, left: 70, right: 70, textAlign: 'center', opacity: e, transform: `translateY(${(1 - e) * 40}px)` }}>
      <div style={{ fontFamily: FONT_DISPLAY, fontWeight: 800, fontSize: size, lineHeight: 1.02, color: '#fff', letterSpacing: -2 }}>
        {text}{accent && <><br /><span style={{ color: C.primary300 }}>{accent}</span></>}
      </div>
    </div>
  );
};

const Hook: React.FC = () => {
  const frame = useCurrentFrame();
  const e = useEnter(0, 10);
  const strike = interpolate(frame, [32, 46], [0, 1], { extrapolateLeft: 'clamp', extrapolateRight: 'clamp', easing: ease });
  const rows = Array.from({ length: 7 });
  return (
    <SceneFade inFrames={1}>
      <Backdrop />
      <Title text="Still estimating" accent="in a spreadsheet?" top={300} size={104} />
      <div style={{ position: 'absolute', left: 140, right: 140, top: 820, opacity: e, transform: `rotate(-4deg) scale(${0.9 + e * 0.1})` }}>
        <div style={{ background: '#fff', borderRadius: 18, padding: 18, boxShadow: '0 30px 80px rgba(0,0,0,.4)' }}>
          {rows.map((_, r) => (
            <div key={r} style={{ display: 'flex', borderBottom: '2px solid #e3e1ec' }}>
              {['A', 'B', 'C', 'D'].map((c, i) => (
                <div key={c} style={{ flex: i === 0 ? 2 : 1, height: 74, borderRight: '2px solid #e3e1ec', fontFamily: FONT_BODY, fontSize: 30, color: r === 0 ? '#888' : '#333', display: 'flex', alignItems: 'center', paddingLeft: 12, background: r === 0 ? '#f3f3f7' : '#fff' }}>
                  {r === 0 ? ['Story', 'Ana', 'Marko', 'Avg'][i] : i === 0 ? `OPP-${100 + r} ...` : ['5', '8', '?', '#REF!'][(r + i) % 4]}
                </div>
              ))}
            </div>
          ))}
        </div>
        <svg viewBox="0 0 100 100" preserveAspectRatio="none" style={{ position: 'absolute', inset: -30, width: 'calc(100% + 60px)', height: 'calc(100% + 60px)' }}>
          <line x1="5" y1="5" x2={5 + 90 * strike} y2={5 + 90 * strike} stroke="#ff4d6d" strokeWidth="3" strokeLinecap="round" />
          <line x1="95" y1="5" x2={95 - 90 * strike} y2={5 + 90 * strike} stroke="#ff4d6d" strokeWidth="3" strokeLinecap="round" />
        </svg>
      </div>
    </SceneFade>
  );
};

const CreateScene: React.FC = () => {
  const frame = useCurrentFrame();
  const { durationInFrames } = useVideoConfig();
  const e = useEnter(2, 16);
  const z = interpolate(frame, [0, durationInFrames], [1.55, 1.3], { easing: ease });
  return (
    <SceneFade>
      <Backdrop />
      <Title text="Create a room" accent="in seconds" />
      <div style={{ position: 'absolute', left: 40, top: 560, opacity: e, transform: `translateY(${(1 - e) * 60}px)` }}>
        <BrowserFrame width={1000}>
          <Zoom zoom={z} fx={0.4} fy={0.35}><HostClip fromMs={H.createDialog - 300} rate={1.2} /></Zoom>
        </BrowserFrame>
      </div>
      <div style={{ position: 'absolute', top: 1300, left: 0, right: 0, textAlign: 'center', fontFamily: FONT_BODY, fontWeight: 700, fontSize: 44, color: C.primary100 }}>
        No sign-up · No passwords
      </div>
    </SceneFade>
  );
};

export const PhoneScene: React.FC<{ title: string; accent: string; fromMs: number; rate?: number }> = ({ title, accent, fromMs, rate = 1 }) => {
  const e = useEnter(2, 15);
  return (
    <SceneFade>
      <Backdrop />
      <Title text={title} accent={accent} top={130} size={84} />
      <div style={{ position: 'absolute', left: '50%', top: 420, transform: `translateX(-50%) translateY(${(1 - e) * 120}px)`, opacity: e }}>
        <PhoneFrame height={1010}><MobileClip fromMs={fromMs} rate={rate} /></PhoneFrame>
      </div>
    </SceneFade>
  );
};

export const RevealScene: React.FC = () => {
  const frame = useCurrentFrame();
  const { durationInFrames } = useVideoConfig();
  const e = useEnter(2, 16);
  const z = interpolate(frame, [0, durationInFrames], [1.45, 1.6], { easing: ease });
  return (
    <SceneFade>
      <Backdrop />
      <Title text="Then all cards" accent="flip at once" />
      <div style={{ position: 'absolute', left: 40, top: 560, opacity: e, transform: `scale(${0.9 + e * 0.1})` }}>
        <BrowserFrame width={1000}>
          <Zoom zoom={z} fx={0.375} fy={0.52}><HostClip fromMs={H.reveal - 400} /></Zoom>
        </BrowserFrame>
      </div>
      <div style={{ position: 'absolute', top: 1300, left: 0, right: 0, textAlign: 'center', fontFamily: FONT_BODY, fontWeight: 700, fontSize: 44, color: C.primary100 }}>
        Average, spread & final estimate
      </div>
    </SceneFade>
  );
};

export const OutroR: React.FC = () => {
  const e = useEnter(2, 12);
  const e2 = useEnter(14, 14);
  return (
    <SceneFade outFrames={14}>
      <Backdrop />
      <AbsoluteFill style={{ alignItems: 'center', justifyContent: 'center', paddingBottom: 260 }}>
        <div style={{ opacity: e, transform: `scale(${0.8 + e * 0.2})` }}><Logo size={230} /></div>
        <div style={{ marginTop: 40, textAlign: 'center', fontFamily: FONT_DISPLAY, fontWeight: 800, fontSize: 110, lineHeight: 1, color: '#fff', letterSpacing: -3, opacity: e }}>
          Open Planning<br /><span style={{ color: C.primary300 }}>Poker</span>
        </div>
        <div style={{ marginTop: 34, fontFamily: FONT_BODY, fontWeight: 700, fontSize: 46, color: '#cfc6f5', opacity: e2 }}>Free · Open source</div>
        <div style={{
          marginTop: 50, padding: '26px 44px', borderRadius: 999, background: C.primary, color: '#fff', fontFamily: FONT_MONO, fontWeight: 700, fontSize: 44,
          boxShadow: '0 20px 60px rgba(106,58,232,.6)', opacity: e2, transform: `translateY(${(1 - e2) * 30}px)`,
        }}>app.openplanningpoker.com</div>
      </AbsoluteFill>
    </SceneFade>
  );
};

export const R_SCENES: Scene[] = [
  { id: 'hook', vo: 'r1', pad: 0.5, render: () => <Hook /> },
  { id: 'create', vo: 'r2', pad: 0.5, render: () => <CreateScene /> },
  { id: 'join', vo: 'r3', pad: 0.4, render: () => <PhoneScene title="Join with a link" accent="right from your phone" fromMs={900} /> },
  { id: 'vote', vo: 'r4', pad: 0.9, render: () => <PhoneScene title="Everyone votes" accent="in secret 🤫" fromMs={MO.voteTap - 1300} /> },
  { id: 'reveal', vo: 'r5', pad: 1.4, render: () => <RevealScene /> },
  { id: 'outro', vo: 'r6', pad: 1.6, render: () => <OutroR /> },
];
const VO_DELAY = 6;
const frames = (s: Scene) => sec(VO[s.vo] + s.pad) + VO_DELAY;
export const R_DURATION = R_SCENES.reduce((a, s) => a + frames(s), 0);

export const Reels: React.FC = () => {
  useFonts();
  const frame = useCurrentFrame();
  const musicVol = interpolate(frame, [0, 15, R_DURATION - 40, R_DURATION], [0, 0.2, 0.2, 0], { extrapolateLeft: 'clamp', extrapolateRight: 'clamp' });
  let start = 0;
  return (
    <AbsoluteFill style={{ background: C.navy }}>
      <Audio src={staticFile('music.wav')} volume={musicVol} />
      {R_SCENES.map((s) => {
        const len = frames(s);
        const from = start;
        start += len;
        return (
          <Sequence key={s.id} from={from} durationInFrames={len} name={s.id}>
            {s.render()}
            <Sequence from={VO_DELAY} layout="none"><Audio src={staticFile(`vo/${s.vo}.mp3`)} /></Sequence>
            {/* Burned-in captions, kept above the platform UI at the bottom. */}
            <Sequence from={VO_DELAY} durationInFrames={sec(VO[s.vo])} layout="none">
              <div style={{ position: 'absolute', left: 80, right: 80, bottom: 330 }}>
                <Subtitle text={TEXT[s.vo].replace('app dot open planning poker dot com', 'app.openplanningpoker.com')} durationInFrames={sec(VO[s.vo])} size={50} />
              </div>
            </Sequence>
          </Sequence>
        );
      })}
    </AbsoluteFill>
  );
};
