import React from 'react';
import {
  AbsoluteFill, Img, OffthreadVideo, continueRender, delayRender, interpolate, spring, staticFile,
  useCurrentFrame, useVideoConfig, Easing,
} from 'remotion';
import marks from '../public/rec/marks.json';
import durations from '../public/vo/durations.json';
import tourMarks from '../public/rec/tour-marks.json';

export const FPS = 30;
export const C = {
  primary: '#6a3ae8', primary300: '#ad94ff', primary100: '#e6ddff', primary50: '#f1ecff',
  ink: '#16132b', navy: '#1c1838', navy2: '#2a2350', bg: '#f5f4fa', white: '#ffffff', amber: '#ffcf70',
};
export const FONT_DISPLAY = "'Bricolage Grotesque', 'Segoe UI', sans-serif";
export const FONT_BODY = "'Figtree', 'Segoe UI', sans-serif";
export const FONT_MONO = "'JetBrains Mono', Consolas, monospace";

export const M = marks as { host: Record<string, number>; mobile: Record<string, number> };
export const VO = durations as Record<string, number>;
/** Marks of the feature tour, per segment (capture/record-tour.mjs). */
export const TOUR = tourMarks as Record<string, Record<string, number>>;
/** Mobile clock = host clock minus this offset (the phone context started later). */
export const MOBILE_OFFSET = M.host.timer - M.mobile.timer;
export const sec = (s: number) => Math.round(s * FPS);

// ---------- Fonts ----------
let fontsRequested = false;
export const useFonts = () => {
  if (fontsRequested) return;
  fontsRequested = true;
  const handle = delayRender('fonts');
  const faces = [
    new FontFace('Bricolage Grotesque', `url(${staticFile('fonts/bricolage-grotesque-latin.woff2')})`, { weight: '200 800' }),
    new FontFace('Figtree', `url(${staticFile('fonts/figtree-latin.woff2')})`, { weight: '300 900' }),
    new FontFace('JetBrains Mono', `url(${staticFile('fonts/jetbrains-mono-latin.woff2')})`, { weight: '100 800' }),
  ];
  Promise.all(faces.map((f) => f.load().then((ff) => document.fonts.add(ff))))
    .then(() => continueRender(handle))
    .catch(() => continueRender(handle));
};

// ---------- Animation helpers ----------
export const useEnter = (delay = 0, damping = 16) => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();
  return spring({ frame: frame - delay, fps, config: { damping, mass: 0.7 } });
};

/** Fades a whole scene in and out. */
export const SceneFade: React.FC<{ children: React.ReactNode; inFrames?: number; outFrames?: number }> = ({ children, inFrames = 8, outFrames = 8 }) => {
  const frame = useCurrentFrame();
  const { durationInFrames } = useVideoConfig();
  const o = interpolate(frame, [0, inFrames, durationInFrames - outFrames, durationInFrames], [0, 1, 1, 0], { extrapolateLeft: 'clamp', extrapolateRight: 'clamp' });
  return <AbsoluteFill style={{ opacity: o }}>{children}</AbsoluteFill>;
};

// ---------- Background ----------
const CARD_VALUES = ['1', '2', '3', '5', '8', '13', '?', '½', '21', '3', '5', '8'];
export const Backdrop: React.FC<{ dark?: boolean; cards?: boolean }> = ({ dark = true, cards = true }) => {
  const frame = useCurrentFrame();
  const { width, height } = useVideoConfig();
  const bg = dark
    ? `radial-gradient(1200px 900px at 20% 10%, #3b2a8f 0%, transparent 60%), radial-gradient(1000px 800px at 90% 90%, #5a2bd0 0%, transparent 55%), linear-gradient(160deg, ${C.navy} 0%, #120f26 100%)`
    : `radial-gradient(1000px 700px at 15% 0%, ${C.primary100} 0%, transparent 60%), radial-gradient(900px 700px at 100% 100%, #ddd2ff 0%, transparent 60%), ${C.bg}`;
  return (
    <AbsoluteFill style={{ background: bg, overflow: 'hidden' }}>
      {cards && CARD_VALUES.map((v, i) => {
        const seed = (i * 9301 + 49297) % 233280 / 233280;
        const x = ((i * 0.618) % 1) * width;
        const baseY = (seed * 1.2 - 0.1) * height;
        const y = baseY - ((frame * (0.25 + seed * 0.35)) % (height * 1.3)) + height * 0.3;
        const yy = ((y % (height * 1.3)) + height * 1.3) % (height * 1.3) - height * 0.15;
        const rot = Math.sin((frame + i * 40) / 90) * 12 + (i % 2 ? 8 : -8);
        const size = 70 + seed * 60;
        return (
          <div key={i} style={{
            position: 'absolute', left: x, top: yy, width: size, height: size * 1.4, borderRadius: size * 0.14,
            transform: `rotate(${rot}deg)`, opacity: dark ? 0.1 : 0.22,
            border: `2px solid ${dark ? '#fff' : C.primary}`, display: 'flex', alignItems: 'center', justifyContent: 'center',
            fontFamily: FONT_DISPLAY, fontWeight: 700, fontSize: size * 0.5, color: dark ? '#fff' : C.primary,
          }}>{v}</div>
        );
      })}
    </AbsoluteFill>
  );
};

// ---------- Video sources ----------
// Playwright records the page at CSS size into the top-left of a larger frame (the rest is grey padding),
// so the video is drawn oversized and anchored top-left: only the page area shows.
type ClipProps = { fromMs: number; rate?: number };
export const Clip: React.FC<ClipProps & { src: string; fw: number; fh: number }> = ({ fromMs, rate = 1, src, fw, fh }) => (
  <OffthreadVideo src={staticFile(src)} trimBefore={Math.max(0, sec(fromMs / 1000))} playbackRate={rate} muted
    style={{ position: 'absolute', left: 0, top: 0, width: `${100 / fw}%`, height: `${100 / fh}%`, objectFit: 'fill' }} />
);
export const HostClip: React.FC<ClipProps> = (p) => <Clip {...p} src="rec/host.webm" fw={1440 / 2160} fh={900 / 1350} />;
/** One segment of the feature tour, recorded like the host. */
export const TourClip: React.FC<ClipProps & { seg: string }> = ({ seg, ...p }) => <Clip {...p} src={`rec/tour-${seg}.webm`} fw={1440 / 2160} fh={900 / 1350} />;
/** iPhone 13 in Playwright: a 390x664 CSS viewport recorded into a 780x1688 frame. */
export const MobileClip: React.FC<ClipProps> = (p) => <Clip {...p} src="rec/mobile.webm" fw={390 / 780} fh={664 / 1688} />;

/**
 * Shows the source zoomed on a focus point. focus is in source fractions (0..1); zoom 1 = fit.
 * The child must fill its box with the full source frame.
 */
export const Zoom: React.FC<{ zoom: number; fx: number; fy: number; children: React.ReactNode }> = ({ zoom, fx, fy, children }) => (
  <div style={{ position: 'absolute', inset: 0, overflow: 'hidden' }}>
    <div style={{
      position: 'absolute', inset: 0, transformOrigin: `${fx * 100}% ${fy * 100}%`, transform: `scale(${zoom})`,
    }}>{children}</div>
  </div>
);

// ---------- Device frames ----------
export const BrowserFrame: React.FC<{ width: number; children: React.ReactNode; style?: React.CSSProperties; dark?: boolean; url?: string }> = ({ width, children, style, dark, url = 'app.openplanningpoker.com' }) => {
  const bar = Math.round(width * 0.028);
  const h = Math.round(width / 1.6);
  return (
    <div style={{
      width, borderRadius: width * 0.012, overflow: 'hidden', background: dark ? '#221d3d' : '#fff',
      boxShadow: '0 40px 100px rgba(10, 5, 40, .45), 0 0 0 1px rgba(255,255,255,.08)', ...style,
    }}>
      <div style={{ height: bar, background: dark ? '#2a2448' : '#ecebf3', display: 'flex', alignItems: 'center', padding: `0 ${bar * 0.5}px`, gap: bar * 0.28 }}>
        {['#ff5f57', '#febc2e', '#28c840'].map((c) => <div key={c} style={{ width: bar * 0.32, height: bar * 0.32, borderRadius: '50%', background: c }} />)}
        <div style={{
          marginLeft: bar * 0.6, flex: 1, maxWidth: width * 0.42, height: bar * 0.62, borderRadius: bar, background: dark ? '#1a1630' : '#fff',
          display: 'flex', alignItems: 'center', paddingLeft: bar * 0.45, fontFamily: FONT_BODY, fontSize: bar * 0.36, color: dark ? '#bdb6dd' : '#6b6787', gap: bar * 0.2,
        }}>
          <svg width={bar * 0.3} height={bar * 0.3} viewBox="0 0 24 24"><path fill="currentColor" d="M12 2a5 5 0 0 0-5 5v3H6a2 2 0 0 0-2 2v8a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2v-8a2 2 0 0 0-2-2h-1V7a5 5 0 0 0-5-5Zm-3 8V7a3 3 0 1 1 6 0v3H9Z" /></svg>
          {url}
        </div>
      </div>
      <div style={{ position: 'relative', width, height: h, overflow: 'hidden', background: C.bg }}>{children}</div>
    </div>
  );
};

export const PhoneFrame: React.FC<{ height: number; children: React.ReactNode; style?: React.CSSProperties }> = ({ height, children, style }) => {
  // Screen = status bar + the 390x664 page viewport + a home-indicator strip.
  const bezel = height * 0.022;
  const screenH = height - bezel * 2;
  const statusH = screenH * 0.07;
  const homeH = screenH * 0.035;
  const pageH = screenH - statusH - homeH;
  const screenW = pageH * (390 / 664);
  const r = screenW * 0.13;
  return (
    <div style={{
      width: screenW + bezel * 2, height, borderRadius: r + bezel, background: '#0d0b18', padding: bezel,
      boxShadow: '0 40px 100px rgba(10,5,40,.5), inset 0 0 0 2px #3a3555', boxSizing: 'border-box', position: 'relative', ...style,
    }}>
      <div style={{ position: 'relative', width: screenW, height: screenH, borderRadius: r, overflow: 'hidden', background: C.bg }}>
        <div style={{ height: statusH, display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: `0 ${screenW * 0.09}px`, fontFamily: FONT_BODY, fontWeight: 700, fontSize: statusH * 0.36, color: C.ink, boxSizing: 'border-box' }}>
          <span>9:41</span>
          <div style={{ width: screenW * 0.3, height: statusH * 0.5, borderRadius: 99, background: '#0d0b18' }} />
          <span style={{ letterSpacing: 2 }}>▮▮▮</span>
        </div>
        <div style={{ position: 'relative', width: screenW, height: pageH, overflow: 'hidden' }}>{children}</div>
        <div style={{ height: homeH, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
          <div style={{ width: screenW * 0.34, height: homeH * 0.18, borderRadius: 99, background: C.ink }} />
        </div>
      </div>
    </div>
  );
};

// ---------- Text ----------
export const Logo: React.FC<{ size: number }> = ({ size }) => (
  <Img src={staticFile('logo.png')} style={{ width: size, height: size, borderRadius: size * 0.22 }} />
);

export const Headline: React.FC<{ text: string; sub?: string; delay?: number; size?: number; color?: string; align?: 'left' | 'center' }> = ({ text, sub, delay = 0, size = 64, color = '#fff', align = 'left' }) => {
  const e = useEnter(delay);
  const e2 = useEnter(delay + 6);
  return (
    <div style={{ textAlign: align }}>
      <div style={{
        fontFamily: FONT_DISPLAY, fontWeight: 800, fontSize: size, color, letterSpacing: -size * 0.02, lineHeight: 1.05,
        opacity: e, transform: `translateY(${(1 - e) * 30}px)`,
      }}>{text}</div>
      {sub && <div style={{
        marginTop: size * 0.25, fontFamily: FONT_BODY, fontWeight: 500, fontSize: size * 0.42, color: color === '#fff' ? '#cfc6f5' : '#5d587a',
        opacity: e2, transform: `translateY(${(1 - e2) * 20}px)`,
      }}>{sub}</div>}
    </div>
  );
};

export const Chip: React.FC<{ text: string; delay?: number; size?: number; style?: React.CSSProperties; icon?: string }> = ({ text, delay = 0, size = 30, style, icon }) => {
  const e = useEnter(delay, 13);
  return (
    <div style={{
      display: 'inline-flex', alignItems: 'center', gap: size * 0.4, padding: `${size * 0.45}px ${size * 0.9}px`, borderRadius: 999,
      background: 'rgba(255,255,255,.96)', color: C.ink, fontFamily: FONT_BODY, fontWeight: 700, fontSize: size,
      boxShadow: '0 18px 40px rgba(20,10,60,.35)', transform: `scale(${0.6 + e * 0.4})`, opacity: e, ...style,
    }}>
      {icon && <span style={{ color: C.primary }}>{icon}</span>}{text}
    </div>
  );
};

/** Burned-in subtitle (used in the vertical video). */
export const Subtitle: React.FC<{ text: string; size?: number; durationInFrames: number }> = ({ text, size = 46, durationInFrames }) => {
  const frame = useCurrentFrame();
  const words = text.split(' ');
  const speakFrames = durationInFrames * 0.85;
  return (
    <div style={{ textAlign: 'center', fontFamily: FONT_DISPLAY, fontWeight: 800, fontSize: size, lineHeight: 1.2, color: '#fff', textShadow: '0 4px 18px rgba(0,0,0,.45)' }}>
      {words.map((w, i) => {
        const on = frame >= (i / words.length) * speakFrames;
        return <span key={i} style={{ color: on ? '#fff' : 'rgba(255,255,255,.45)', transition: 'none' }}>{w} </span>;
      })}
    </div>
  );
};

export const ease = Easing.bezier(0.45, 0, 0.2, 1);
