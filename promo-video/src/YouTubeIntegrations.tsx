// YouTube 16:9 promo with the integrations: the core game, then Jira, Slack and the Microsoft Teams tab.
import React from 'react';
import { AbsoluteFill, Audio, Sequence, interpolate, staticFile, useCurrentFrame, useVideoConfig } from 'remotion';
import { Backdrop, BrowserFrame, C, Chip, FONT_BODY, Headline, HostClip, M, MOBILE_OFFSET, SceneFade, VO, Zoom, sec, useFonts } from './shared';
import { DesktopScene, Features, Intro, Outro, SplitScene, featuresFrom } from './YouTube';
import { ChatWindow, JiraImport, JiraIssueCard, Pointer, Scaled, TM, TeamsConfigDialog, TeamsMeeting } from './mocks';

const H = M.host;
type Scene = { id: string; vo: string; pad: number; render: (len: number) => React.ReactNode };

const Top: React.FC<{ title: string; sub?: string }> = ({ title, sub }) => (
  <div style={{ position: 'absolute', left: 120, top: 58, right: 120 }}><Headline text={title} sub={sub} size={58} /></div>
);

const SideChips: React.FC<{ items: { text: string; at: number; icon?: string }[]; left: number; top: number; gap?: number }> = ({ items, left, top, gap = 96 }) => (
  <>
    {items.map((c, i) => (
      <Sequence key={i} from={c.at} layout="none">
        <div style={{ position: 'absolute', left, top: top + i * gap }}><Chip text={c.text} icon={c.icon} size={29} /></div>
      </Sequence>
    ))}
  </>
);

const JiraImportScene: React.FC = () => {
  const { durationInFrames: len } = useVideoConfig();
  const click = len - 62;
  // Dialog at (140, 225); the Import button sits near its bottom-right corner.
  return (
    <SceneFade>
      <Backdrop />
      <Top title="Plan straight from Jira" sub="Import a sprint, a board or a JQL query." />
      <div style={{ position: 'absolute', left: 140, top: 225 }}><JiraImport listAt={22} clickAt={click} toastAt={click + 6} /></div>
      <SideChips left={1120} top={330} items={[
        { text: 'Connect your Jira Cloud site once', at: 30, icon: '🔗' },
        { text: 'Sprint, board or JQL', at: 60, icon: '🗂' },
        { text: 'Issues become tickets', at: click + 8, icon: '🃏' },
      ]} />
      <Pointer points={[[click - 40, 1250, 1010], [click - 6, 942, 888]]} clicks={[click]} />
    </SceneFade>
  );
};

const JiraSaveScene: React.FC = () => {
  const frame = useCurrentFrame();
  const click = 46;
  const pressed = frame >= click && frame < click + 8;
  return (
    <SceneFade>
      <Backdrop />
      <Top title="Estimates go back to Jira" sub="One click fills in the story points." />
      <div style={{ position: 'absolute', left: 90, top: 225 }}>
        <BrowserFrame width={960}>
          <Zoom zoom={1.25} fx={0.5} fy={0.62}><HostClip fromMs={H.final - 900} /></Zoom>
        </BrowserFrame>
      </div>
      <div style={{
        position: 'absolute', left: 90 + 480 - 170, top: 905, width: 340, textAlign: 'center', padding: '18px 0', borderRadius: 999, background: pressed ? '#5228c4' : C.primary,
        color: '#fff', fontFamily: FONT_BODY, fontWeight: 800, fontSize: 30, boxShadow: '0 18px 40px rgba(20,10,60,.45)', transform: pressed ? 'scale(.96)' : 'none',
      }}>Save to OPP-101</div>
      <div style={{ position: 'absolute', left: 1110, top: 300 }}><Scaled w={760} h={560} scale={1}><JiraIssueCard savedAt={click + 8} /></Scaled></div>
      <Sequence from={click + 10} layout="none">
        <div style={{ position: 'absolute', left: 1110, top: 890 }}><Chip text="Estimate saved to OPP-101." icon="✓" size={29} /></div>
      </Sequence>
      <Pointer points={[[click - 30, 900, 760], [click - 6, 600, 940]]} clicks={[click]} />
    </SceneFade>
  );
};

const SlackScene: React.FC<{ thread?: boolean }> = ({ thread }) => (
  <SceneFade>
    <Backdrop />
    {thread
      ? <Top title="Results land in the thread" sub="Only you get the host link." />
      : <Top title="Start from Slack" sub="Type /poker in any channel." />}
    <div style={{ position: 'absolute', left: (1920 - 1500 * 0.98) / 2, top: 222 }}>
      <Scaled w={1500} h={860} scale={0.98}>
        {thread
          ? <ChatWindow startPosted ephemeralAt={-60} threadAt={18} replyAt={70} />
          : <ChatWindow typeFrom={18} postAt={96} ephemeralAt={140} />}
      </Scaled>
    </div>
  </SceneFade>
);

const TeamsConfigScene: React.FC = () => {
  const { durationInFrames: len } = useVideoConfig();
  const from = TM.cfg.open - 300;
  const rate = (TM.cfg.saved + 500 - from) / 1000 / ((len - 16) / 30);
  const saveAt = sec((TM.cfg.save - from) / 1000 / rate);
  return (
    <SceneFade>
      <Backdrop />
      <Top title="Add it to Microsoft Teams" sub="A tab in a meeting, a channel or a chat." />
      <div style={{ position: 'absolute', left: 240, top: 228 }}>
        <Scaled w={1600} h={900} scale={0.9}>
          <TeamsMeeting panelFromMs={0} stage="tiles" dim panel={false}>
            <TeamsConfigDialog fromMs={from} rate={rate} saveAt={saveAt} style={{ left: 440, top: 110 }} />
            <Pointer points={[[saveAt - 16, 1080, 690], [saveAt - 3, 1080, 766]]} clicks={[saveAt]} size={34} />
          </TeamsMeeting>
        </Scaled>
      </div>
    </SceneFade>
  );
};

const TeamsMeetingScene: React.FC = () => {
  const { durationInFrames: len } = useVideoConfig();
  const from = TM.panel.votesStart - 600;
  const rate = (TM.panel.reveal + 2600 - from) / 1000 / (len / 30);
  return (
    <SceneFade>
      <Backdrop />
      <Top title="Vote without leaving the call" sub="In the meeting side panel or on the shared stage." />
      <div style={{ position: 'absolute', left: 240, top: 228 }}>
        <Scaled w={1600} h={900} scale={0.9}><TeamsMeeting panelFromMs={from} rate={rate} stage="app" /></Scaled>
      </div>
      <SideChips left={60} top={1000} gap={0} items={[{ text: 'Meeting stage', at: 20, icon: '🖥' }]} />
      <SideChips left={1395} top={1000} gap={0} items={[{ text: 'Side panel', at: 30, icon: '▮' }]} />
    </SceneFade>
  );
};

const featuresRate = (H.end - featuresFrom) / 1000 / (VO.f1 + 0.9);
export const SCENES_I: Scene[] = [
  { id: 'intro', vo: 'y1', pad: 1.3, render: () => <Intro /> },
  { id: 'create', vo: 'y2', pad: 0.6, render: () => <DesktopScene title="Create a room in seconds" sub="No sign-up. No passwords." fromMs={H.createDialog - 400} rate={1.05} chips={[{ text: 'Fibonacci, T-shirt or custom decks', at: 40, icon: '🃏' }]} /> },
  { id: 'vote', vo: 'y6', pad: 0.6, render: () => <SplitScene title="Vote in secret" sub="See who voted, never what. Nobody anchors the team." fromMs={H.timer - 400} phoneFromMs={H.timer - 400 - MOBILE_OFFSET} chips={[{ text: 'Shared timer for the room', at: 15, icon: '⏱' }]} /> },
  { id: 'reveal', vo: 'y7', pad: 1.0, render: () => <SplitScene title="Flip all cards at once" fromMs={H.reveal - 500} phoneFromMs={H.reveal - 500 - MOBILE_OFFSET} zoom={{ from: 1, to: 1.45, fx: 0.37, fy: 0.55 }} /> },
  { id: 'jira-import', vo: 'j1', pad: 0.8, render: () => <JiraImportScene /> },
  { id: 'jira-save', vo: 'j2', pad: 0.9, render: () => <JiraSaveScene /> },
  { id: 'slack-start', vo: 's1', pad: 0.8, render: () => <SlackScene /> },
  { id: 'slack-thread', vo: 's2', pad: 0.8, render: () => <SlackScene thread /> },
  { id: 'teams-config', vo: 't1', pad: 0.6, render: () => <TeamsConfigScene /> },
  { id: 'teams-meeting', vo: 't2', pad: 0.8, render: () => <TeamsMeetingScene /> },
  { id: 'features', vo: 'f1', pad: 0.9, render: () => <Features fromMs={featuresFrom} rate={featuresRate} title="And everything else a session needs" /> },
  { id: 'outro', vo: 'o1', pad: 1.8, render: () => <Outro tagline="Free · Open source · In your browser, Slack, Teams and Jira" /> },
];
const VO_DELAY = 8;
export const sceneFramesI = (s: Scene) => sec(VO[s.vo] + s.pad) + VO_DELAY;
export const YTI_DURATION = SCENES_I.reduce((a, s) => a + sceneFramesI(s), 0);
/** Narration start/end in seconds, for the .srt file. */
export const narrationTimesI = () => {
  let start = 0;
  return SCENES_I.map((s) => {
    const from = start;
    start += sceneFramesI(s);
    return { vo: s.vo, start: (from + VO_DELAY) / 30, end: (from + VO_DELAY) / 30 + VO[s.vo] };
  });
};

export const YouTubeIntegrations: React.FC = () => {
  useFonts();
  const frame = useCurrentFrame();
  let start = 0;
  const musicVol = interpolate(frame, [0, 30, YTI_DURATION - 60, YTI_DURATION], [0, 0.16, 0.16, 0], { extrapolateLeft: 'clamp', extrapolateRight: 'clamp' });
  return (
    <AbsoluteFill style={{ background: C.navy }}>
      <Audio src={staticFile('music.wav')} volume={musicVol} loop />
      {SCENES_I.map((s) => {
        const len = sceneFramesI(s);
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
