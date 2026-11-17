// Instagram Reels / TikTok 9:16 promo with the integrations: Slack, the Teams tab and Jira, then the vote and reveal.
import React from 'react';
import { AbsoluteFill, Audio, Sequence, interpolate, staticFile, useCurrentFrame } from 'remotion';
import { Backdrop, C, FONT_BODY, FONT_DISPLAY, M, SceneFade, Subtitle, VO, sec, useEnter, useFonts } from './shared';
import { OutroR, PhoneScene, RevealScene, Title } from './Reels';
import { ChatWindow, JiraImport, JiraIssueCard, Scaled, TM, TeamsPanelClip, ThreadReplyCard } from './mocks';
import narration from '../capture/narration.json';

const MO = M.mobile;
const TEXT = narration as Record<string, string>;
type Scene = { id: string; vo: string; pad: number; render: () => React.ReactNode };

const Pill: React.FC<{ text: string; icon: string; delay: number }> = ({ text, icon, delay }) => {
  const e = useEnter(delay, 12);
  return (
    <div style={{
      display: 'flex', alignItems: 'center', gap: 22, padding: '28px 46px', borderRadius: 999, background: 'rgba(255,255,255,.96)', color: C.ink,
      fontFamily: FONT_DISPLAY, fontWeight: 800, fontSize: 62, boxShadow: '0 20px 50px rgba(20,10,60,.4)', opacity: e, transform: `scale(${0.6 + e * 0.4})`,
    }}><span style={{ color: C.primary }}>{icon}</span>{text}</div>
  );
};

const Hook: React.FC = () => (
  <SceneFade inFrames={1}>
    <Backdrop />
    <Title text="Planning poker" accent="where your team works" top={300} size={100} />
    <div style={{ position: 'absolute', top: 760, left: 0, right: 0, display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 40 }}>
      <Pill text="Slack" icon="#" delay={14} />
      <Pill text="Microsoft Teams" icon="▦" delay={22} />
      <Pill text="Jira" icon="◆" delay={30} />
    </div>
  </SceneFade>
);

const SlackR: React.FC = () => {
  const e = useEnter(2, 16);
  return (
    <SceneFade>
      <Backdrop />
      <Title text="Type /poker" accent="in Slack" top={130} size={88} />
      <div style={{ position: 'absolute', left: (1080 - 760) / 2, top: 390, opacity: e, transform: `translateY(${(1 - e) * 60}px)` }}>
        <Scaled w={760} h={1020} scale={1}><ChatWindow compact typeFrom={6} postAt={62} /></Scaled>
      </div>
    </SceneFade>
  );
};

const Tile: React.FC<{ name: string; color: string }> = ({ name, color }) => (
  <div style={{ width: 200, height: 170, borderRadius: 18, background: '#2b2a3d', display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', gap: 10, boxShadow: '0 20px 50px rgba(0,0,0,.35)' }}>
    <div style={{ width: 84, height: 84, borderRadius: '50%', background: color, color: '#fff', display: 'flex', alignItems: 'center', justifyContent: 'center', fontFamily: FONT_BODY, fontWeight: 800, fontSize: 40 }}>{name[0]}</div>
    <div style={{ fontFamily: FONT_BODY, fontWeight: 600, fontSize: 24, color: '#e8e6f4' }}>{name}</div>
  </div>
);

const TeamsR: React.FC = () => {
  const e = useEnter(2, 16);
  const s = 1.4;
  return (
    <SceneFade>
      <Backdrop />
      <Title text="Or a tab in" accent="your Teams meeting" top={130} size={88} />
      <div style={{ position: 'absolute', left: 40, top: 520, display: 'flex', flexDirection: 'column', gap: 22 }}>
        <Tile name="Bojan" color="#6a3ae8" /><Tile name="Ana" color="#d24b8f" />
      </div>
      <div style={{ position: 'absolute', right: 40, top: 620, display: 'flex', flexDirection: 'column', gap: 22 }}>
        <Tile name="Marko" color="#2e9e8f" /><Tile name="Jelena" color="#d9822b" />
      </div>
      <div style={{ position: 'absolute', left: (1080 - 360 * s) / 2, top: 400, opacity: e, transform: `translateY(${(1 - e) * 80}px)`, borderRadius: 22, overflow: 'hidden', background: '#fff', boxShadow: '0 40px 100px rgba(10,5,40,.5)' }}>
        <div style={{ height: 60, display: 'flex', alignItems: 'center', gap: 12, padding: '0 20px', fontFamily: FONT_BODY, fontWeight: 800, fontSize: 24, color: C.ink, borderBottom: '1px solid #ecebf3' }}>
          Meeting side panel <span style={{ marginLeft: 'auto', color: '#8a86a0' }}>✕</span>
        </div>
        <Scaled w={360} h={720} scale={s}><div style={{ position: 'relative', width: 360, height: 760 }}><TeamsPanelClip fromMs={TM.panel.timer - 1200} rate={1.6} /></div></Scaled>
      </div>
    </SceneFade>
  );
};

const JiraR: React.FC = () => (
  <SceneFade>
    <Backdrop />
    <Title text="Pull the sprint" accent="from Jira" top={130} size={88} />
    <div style={{ position: 'absolute', left: (1080 - 900 * 1.08) / 2, top: 480 }}>
      <Scaled w={900} h={720} scale={1.08}><JiraImport listAt={6} clickAt={56} toastAt={60} /></Scaled>
    </div>
  </SceneFade>
);

const ResultsR: React.FC = () => (
  <SceneFade>
    <Backdrop />
    <Title text="Back to Jira," accent="and into the thread" top={130} size={88} />
    <div style={{ position: 'absolute', left: (1080 - 760 * 1.17) / 2, top: 400 }}>
      <Scaled w={760} h={560} scale={1.17}><JiraIssueCard savedAt={18} /></Scaled>
    </div>
    <div style={{ position: 'absolute', left: 90, top: 1100 }}>
      <Scaled w={900} h={240} scale={1}><ThreadReplyCard at={62} /></Scaled>
    </div>
  </SceneFade>
);

export const RI_SCENES: Scene[] = [
  { id: 'hook', vo: 'ir1', pad: 0.5, render: () => <Hook /> },
  { id: 'slack', vo: 'ir2', pad: 0.5, render: () => <SlackR /> },
  { id: 'teams', vo: 'ir3', pad: 0.7, render: () => <TeamsR /> },
  { id: 'jira', vo: 'ir4', pad: 0.8, render: () => <JiraR /> },
  { id: 'vote', vo: 'r4', pad: 0.9, render: () => <PhoneScene title="Everyone votes" accent="in secret 🤫" fromMs={MO.voteTap - 1300} /> },
  { id: 'reveal', vo: 'r5', pad: 1.4, render: () => <RevealScene /> },
  { id: 'results', vo: 'ir5', pad: 0.9, render: () => <ResultsR /> },
  { id: 'outro', vo: 'r6', pad: 1.6, render: () => <OutroR /> },
];
const VO_DELAY = 6;
const frames = (s: Scene) => sec(VO[s.vo] + s.pad) + VO_DELAY;
export const RI_DURATION = RI_SCENES.reduce((a, s) => a + frames(s), 0);

export const ReelsIntegrations: React.FC = () => {
  useFonts();
  const frame = useCurrentFrame();
  const musicVol = interpolate(frame, [0, 15, RI_DURATION - 40, RI_DURATION], [0, 0.2, 0.2, 0], { extrapolateLeft: 'clamp', extrapolateRight: 'clamp' });
  let start = 0;
  return (
    <AbsoluteFill style={{ background: C.navy }}>
      <Audio src={staticFile('music.wav')} volume={musicVol} />
      {RI_SCENES.map((s) => {
        const len = frames(s);
        const from = start;
        start += len;
        return (
          <Sequence key={s.id} from={from} durationInFrames={len} name={s.id}>
            {s.render()}
            <Sequence from={VO_DELAY} layout="none"><Audio src={staticFile(`vo/${s.vo}.mp3`)} /></Sequence>
            <Sequence from={VO_DELAY} durationInFrames={sec(VO[s.vo])} layout="none">
              <div style={{ position: 'absolute', left: 80, right: 80, bottom: 330 }}>
                <Subtitle text={TEXT[s.vo].replace('app dot open planning poker dot com', 'app.openplanningpoker.com').replace('slash poker', '/poker')} durationInFrames={sec(VO[s.vo])} size={50} />
              </div>
            </Sequence>
          </Sequence>
        );
      })}
    </AbsoluteFill>
  );
};
