// The full feature tour (v3.8.0): planning poker, sorting, discussion, recap, team room, public API, retro,
// organizations and integrations. 16:9 for YouTube; TourReels is the 9:16 cut for TikTok, Reels and Shorts.
import React from 'react';
import { AbsoluteFill, Audio, Sequence, interpolate, staticFile, useCurrentFrame, useVideoConfig } from 'remotion';
import {
  Backdrop, BrowserFrame, C, Chip, FONT_BODY, Headline, M, MOBILE_OFFSET, SceneFade, Subtitle, TOUR, TourClip, VO, Zoom,
  ease, sec, useEnter, useFonts,
} from './shared';
import { DesktopScene, Features, Intro, Outro, SplitScene, featuresFrom } from './YouTube';
import { JiraImportScene, SlackScene, TeamsMeetingScene } from './YouTubeIntegrations';
import { CreateScene, OutroR, PhoneScene, RevealScene, Title } from './Reels';
import { TeamsR } from './ReelsIntegrations';
import narration from '../capture/narration.json';

const H = M.host;
const TEXT = narration as Record<string, string>;
type Part = { seg: string; from: number; to: number };
type Scene = { id: string; vo: string; pad: number; render: () => React.ReactNode };
const part = (seg: string, from: string, fromOff: number, to: string, toOff: number): Part =>
  ({ seg, from: TOUR[seg][from] + fromOff, to: TOUR[seg][to] + toOff });

/** Plays recorded parts one after the other, all at the speed that fits them into the scene. */
const Parts: React.FC<{ parts: Part[] }> = ({ parts }) => {
  const { durationInFrames, fps } = useVideoConfig();
  const total = parts.reduce((a, p) => a + p.to - p.from, 0);
  const rate = Math.min(2.4, Math.max(0.75, total / 1000 / (durationInFrames / fps)));
  let start = 0;
  return (
    <>
      {parts.map((p, i) => {
        const len = i === parts.length - 1 ? durationInFrames - start : Math.round(((p.to - p.from) / 1000 / rate) * fps);
        const from = start;
        start += len;
        return (
          <Sequence key={i} from={from} durationInFrames={len} layout="none">
            <AbsoluteFill><TourClip seg={p.seg} fromMs={p.from} rate={rate} /></AbsoluteFill>
          </Sequence>
        );
      })}
    </>
  );
};

type ZoomSpec = { from: number; to: number; fx: number; fy: number };
const useZoom = (zoom?: ZoomSpec) => {
  const frame = useCurrentFrame();
  const { durationInFrames } = useVideoConfig();
  return zoom ? interpolate(frame, [0, durationInFrames], [zoom.from, zoom.to], { easing: ease, extrapolateRight: 'clamp' }) : 1;
};

/** A tour clip in a browser frame under a headline, with chips on the right. */
const TourScene: React.FC<{ title: string; sub?: string; parts: Part[]; url?: string; chips?: { text: string; at: number; icon?: string }[]; zoom?: ZoomSpec }> = ({ title, sub, parts, url, chips = [], zoom }) => {
  const enter = useEnter(2, 18);
  const z = useZoom(zoom);
  const width = chips.length ? 1250 : 1380;
  const left = chips.length ? 90 : (1920 - 1380) / 2;
  return (
    <SceneFade>
      <Backdrop />
      <div style={{ position: 'absolute', left: 110, top: 58, right: 110 }}><Headline text={title} sub={sub} size={58} /></div>
      <div style={{ position: 'absolute', left, top: 225, transform: `translateY(${(1 - enter) * 60}px)`, opacity: enter }}>
        <BrowserFrame width={width} url={url}>
          <Zoom zoom={z} fx={zoom?.fx ?? 0.5} fy={zoom?.fy ?? 0.5}><Parts parts={parts} /></Zoom>
        </BrowserFrame>
      </div>
      {chips.map((c, i) => (
        <Sequence key={i} from={c.at} layout="none">
          <div style={{ position: 'absolute', left: 1385, top: 270 + i * 112 }}><Chip text={c.text} icon={c.icon} size={28} /></div>
        </Sequence>
      ))}
    </SceneFade>
  );
};

const g = 'game', t = 'team', r = 'retro';
const featuresRate = (H.end - featuresFrom) / 1000 / (VO.f2 + 0.9);
export const TOUR_SCENES: Scene[] = [
  { id: 'intro', vo: 'w1', pad: 1.0, render: () => <Intro /> },
  { id: 'create', vo: 'y2', pad: 0.5, render: () => <DesktopScene title="Create a room in seconds" sub="No sign-up. No passwords." fromMs={H.createDialog - 400} rate={1.05} chips={[{ text: 'Fibonacci, T-shirt or custom decks', at: 40, icon: '🃏' }]} /> },
  { id: 'import', vo: 'y3', pad: 0.5, render: () => <DesktopScene title="Paste your backlog" sub="Every line becomes a ticket." fromMs={H.importDialog - 300} rate={2.2} /> },
  { id: 'invite', vo: 'y4', pad: 0.5, render: () => <DesktopScene title="Invite your team" sub="Share the link or scan the QR code." fromMs={H.invite - 500} zoom={{ from: 1, to: 1.25, fx: 0.5, fy: 0.42 }} /> },
  { id: 'join', vo: 'y5', pad: 0.5, render: () => <SplitScene title="Join from any device" sub="Laptop or phone, right in the browser." fromMs={H.joinStart} rate={2.2} phoneFromMs={1300} /> },
  { id: 'vote', vo: 'y6', pad: 0.5, render: () => <SplitScene title="Vote in secret" sub="See who voted, never what. Nobody anchors the team." fromMs={H.timer - 400} phoneFromMs={H.timer - 400 - MOBILE_OFFSET} chips={[{ text: 'Shared timer for the room', at: 15, icon: '⏱' }]} /> },
  { id: 'reveal', vo: 'y7', pad: 0.8, render: () => <SplitScene title="Flip all cards at once" fromMs={H.reveal - 500} phoneFromMs={H.reveal - 500 - MOBILE_OFFSET} zoom={{ from: 1, to: 1.45, fx: 0.37, fy: 0.55 }} /> },
  { id: 'results', vo: 'y8', pad: 0.5, render: () => <DesktopScene title="Talk about the outliers" sub="Average, median, spread and the final estimate." fromMs={H.results - 400} chips={[{ text: 'Wide spread? Ask who voted 3 and 8', at: 30, icon: '💬' }, { text: 'Record the final estimate', at: sec((H.final - H.results) / 1000), icon: '✔' }]} /> },
  { id: 'sorting', vo: 'w2', pad: 0.6, render: () => <TourScene title="Sort a long backlog" sub="Silently, onto card columns. Disputed tickets get a “Discuss” mark." parts={[part(g, 'sorting', -800, 'contested', 2500)]} zoom={{ from: 1.15, to: 1.3, fx: 0.25, fy: 0.25 }} /> },
  { id: 'discussion', vo: 'w3', pad: 0.6, render: () => <TourScene title="Run the discussion" sub="Notes from the outliers, reactions and a confidence check." parts={[part(g, 'reveal', -300, 'notes', 2500), part(g, 'final', -200, 'confidence', 1200)]}
    chips={[{ text: 'Lowest and highest voters say why', at: 40, icon: '💬' }, { text: 'Reactions', at: 110, icon: '👍' }, { text: 'How sure is the team?', at: 170, icon: '✋' }]} /> },
  { id: 'recap', vo: 'w4', pad: 0.8, render: () => <TourScene title="Celebrate, then share the recap" sub="A read-only link with every estimate and the team's numbers." parts={[part(g, 'consensus', -300, 'consensus', 3300), part(g, 'recap', -200, 'end', 0)]} /> },
  { id: 'team', vo: 'w5', pad: 0.6, render: () => <TourScene title="Every sprint in a team room" sub="Velocity, history and a sprint plan that fits who is here." parts={[part(t, 'velocity', -400, 'history', 1800), part(t, 'plan', 200, 'planned', 1500)]}
    chips={[{ text: 'Velocity chart', at: 20, icon: '📈' }, { text: 'Sprint history', at: 75, icon: '🗂' }, { text: 'Capacity & point budget', at: 140, icon: '🧮' }]} /> },
  { id: 'api', vo: 'w6', pad: 0.6, render: () => <TourScene title="Build on your own data" sub="Session numbers, API tokens and signed webhooks." parts={[part(t, 'insights', -300, 'insights', 2400), part(t, 'api', -200, 'end', 0)]}
    chips={[{ text: 'GET /api/v1/sessions', at: 120, icon: '⌁' }, { text: 'Webhooks: ticket.estimated', at: 180, icon: '🔔' }]} /> },
  { id: 'docs', vo: 'w7', pad: 0.6, render: () => <TourScene title="A documented public API" sub="Build dashboards, bots and integrations, or just practise." url="openplanningpoker.com/public-api/" parts={[part('api', 'docs', -800, 'docsEnd', 600)]} /> },
  { id: 'retro', vo: 'w8', pad: 0.8, render: () => <TourScene title="Run the retro in the same place" sub="Write, reveal, group, vote with dots, agree on action items." parts={[part(r, 'retroWrite', -3200, 'retroWrite', 2800), part(r, 'retroGroup', -600, 'retroGroup', 2400), part(r, 'retroVoting', 900, 'retroVoting', 5200), part(r, 'retroAction', -1800, 'retroAction', 1500), part(r, 'retroFacts', 300, 'end', 0)]} /> },
  { id: 'org', vo: 'w9', pad: 0.6, render: () => <TourScene title="Organizations" sub="Several team rooms side by side, ready to download as CSV." parts={[part('org', 'org', -800, 'end', 0)]} zoom={{ from: 1, to: 1.15, fx: 0.5, fy: 0.55 }} /> },
  { id: 'jira', vo: 'j1', pad: 0.6, render: () => <JiraImportScene /> },
  { id: 'slack', vo: 's1', pad: 0.6, render: () => <SlackScene /> },
  { id: 'teams', vo: 't2', pad: 0.6, render: () => <TeamsMeetingScene /> },
  { id: 'features', vo: 'f2', pad: 0.9, render: () => <Features fromMs={featuresFrom} rate={featuresRate} title="And everything else a session needs" /> },
  { id: 'outro', vo: 'o1', pad: 1.8, render: () => <Outro tagline="Free · Open source · In your browser, Slack, Teams and Jira" /> },
];
const VO_DELAY = 8;
export const tourFrames = (s: Scene) => sec(VO[s.vo] + s.pad) + VO_DELAY;
export const TOUR_DURATION = TOUR_SCENES.reduce((a, s) => a + tourFrames(s), 0);
/** Narration start/end in seconds, for the .srt file. */
export const narrationTimesTour = () => {
  let start = 0;
  return TOUR_SCENES.map((s) => {
    const from = start;
    start += tourFrames(s);
    return { vo: s.vo, start: (from + VO_DELAY) / 30, end: (from + VO_DELAY) / 30 + VO[s.vo] };
  });
};

export const Tour: React.FC = () => {
  useFonts();
  const frame = useCurrentFrame();
  const musicVol = interpolate(frame, [0, 30, TOUR_DURATION - 60, TOUR_DURATION], [0, 0.14, 0.14, 0], { extrapolateLeft: 'clamp', extrapolateRight: 'clamp' });
  let start = 0;
  return (
    <AbsoluteFill style={{ background: C.navy }}>
      <Audio src={staticFile('music.wav')} volume={musicVol} loop />
      {TOUR_SCENES.map((s) => {
        const len = tourFrames(s);
        const from = start;
        start += len;
        return (
          <Sequence key={s.id} from={from} durationInFrames={len} name={s.id}>
            {s.render()}
            <Sequence from={VO_DELAY} layout="none"><Audio src={staticFile(`vo/${s.vo}.mp3`)} /></Sequence>
          </Sequence>
        );
      })}
    </AbsoluteFill>
  );
};

// ---------- Vertical cut ----------
/** A zoomed tour clip for the vertical video. */
const ReelClip: React.FC<{ title: string; accent: string; parts: Part[]; zoom: ZoomSpec; caption?: string; url?: string }> = ({ title, accent, parts, zoom, caption, url }) => {
  const e = useEnter(2, 16);
  const z = useZoom(zoom);
  return (
    <SceneFade>
      <Backdrop />
      <Title text={title} accent={accent} />
      <div style={{ position: 'absolute', left: 40, top: 560, opacity: e, transform: `translateY(${(1 - e) * 60}px)` }}>
        <BrowserFrame width={1000} url={url}>
          <Zoom zoom={z} fx={zoom.fx} fy={zoom.fy}><Parts parts={parts} /></Zoom>
        </BrowserFrame>
      </div>
      {caption && <div style={{ position: 'absolute', top: 1300, left: 0, right: 0, textAlign: 'center', fontFamily: FONT_BODY, fontWeight: 700, fontSize: 44, color: C.primary100 }}>{caption}</div>}
    </SceneFade>
  );
};

const HookR: React.FC = () => (
  <ReelClip title="From planning poker" accent="to the retro" parts={[part(g, 'consensus', -600, 'consensus', 3600)]} zoom={{ from: 1.45, to: 1.6, fx: 0.3, fy: 0.42 }} caption="One free tool for the whole sprint" />
);

export const TOUR_R_SCENES: Scene[] = [
  { id: 'hook', vo: 'k1', pad: 0.5, render: () => <HookR /> },
  { id: 'create', vo: 'r2', pad: 0.4, render: () => <CreateScene /> },
  { id: 'join', vo: 'r3', pad: 0.3, render: () => <PhoneScene title="Join with a link" accent="right from your phone" fromMs={900} /> },
  { id: 'reveal', vo: 'r5', pad: 0.9, render: () => <RevealScene /> },
  { id: 'sorting', vo: 'k2', pad: 1.0, render: () => <ReelClip title="Sort the backlog" accent="in silence" parts={[part(g, 'sorting', 3000, 'contested', 1500)]} zoom={{ from: 1.6, to: 1.75, fx: 0.15, fy: 0.3 }} caption="Disputed tickets get a “Discuss” mark" /> },
  { id: 'team', vo: 'k3', pad: 1.0, render: () => <ReelClip title="Velocity and" accent="a sprint plan" parts={[part(t, 'velocity', 0, 'velocity', 2500), part(t, 'plan', 3500, 'planned', 1000)]} zoom={{ from: 1.55, to: 1.65, fx: 0.25, fy: 0.3 }} caption="The point budget fits who is here" /> },
  { id: 'retro', vo: 'k4', pad: 1.0, render: () => <ReelClip title="Retros with dots" accent="and action items" parts={[part(r, 'retroGroup', -300, 'retroGroup', 2000), part(r, 'retroResults', 200, 'retroResults', 2500)]} zoom={{ from: 1.5, to: 1.6, fx: 0.4, fy: 0.45 }} caption="Write · Group · Vote · Discuss" /> },
  { id: 'org', vo: 'k5', pad: 1.0, render: () => <ReelClip title="Compare teams," accent="build on the API" parts={[part('org', 'compare', 300, 'compare', 2600), part(t, 'apiToken', -1500, 'apiToken', 1500)]} zoom={{ from: 1.45, to: 1.55, fx: 0.3, fy: 0.35 }} caption="Tokens and signed webhooks" /> },
  { id: 'teams', vo: 'k6', pad: 0.9, render: () => <TeamsR /> },
  { id: 'outro', vo: 'r6', pad: 1.6, render: () => <OutroR /> },
];
const VO_DELAY_R = 6;
const framesR = (s: Scene) => sec(VO[s.vo] + s.pad) + VO_DELAY_R;
export const TOUR_R_DURATION = TOUR_R_SCENES.reduce((a, s) => a + framesR(s), 0);

export const TourReels: React.FC = () => {
  useFonts();
  const frame = useCurrentFrame();
  const musicVol = interpolate(frame, [0, 15, TOUR_R_DURATION - 40, TOUR_R_DURATION], [0, 0.2, 0.2, 0], { extrapolateLeft: 'clamp', extrapolateRight: 'clamp' });
  let start = 0;
  return (
    <AbsoluteFill style={{ background: C.navy }}>
      <Audio src={staticFile('music.wav')} volume={musicVol} loop />
      {TOUR_R_SCENES.map((s) => {
        const len = framesR(s);
        const from = start;
        start += len;
        return (
          <Sequence key={s.id} from={from} durationInFrames={len} name={s.id}>
            {s.render()}
            <Sequence from={VO_DELAY_R} layout="none"><Audio src={staticFile(`vo/${s.vo}.mp3`)} /></Sequence>
            <Sequence from={VO_DELAY_R} durationInFrames={sec(VO[s.vo])} layout="none">
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
