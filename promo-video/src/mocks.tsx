// Drawn stand-ins for Slack, Jira and the Teams meeting window (OPP-10, OPP-22, OPP-23). Our own app inside them is
// real footage; the chat, tracker and meeting chrome are simplified drawings, not copies of those products' UI.
// The texts match what the integrations actually post (ChatIntegrations.cs, ChatGameService.ResultText, i18n en.ts).
import React from 'react';
import { interpolate, spring, useCurrentFrame, useVideoConfig } from 'remotion';
import { C, Clip, FONT_BODY, FONT_DISPLAY, FONT_MONO, Logo, ease } from './shared';
import teamsMarks from '../public/rec/teams-marks.json';

export const TM = teamsMarks as unknown as {
  cfg: Record<string, number>; panel: Record<string, number>; stageOffset: number;
};

// ---------- Real clips of the Teams tab (recorded at CSS size, so the frame is the page) ----------
type ClipP = { fromMs: number; rate?: number };
export const TeamsConfigClip: React.FC<ClipP> = (p) => <Clip {...p} src="rec/teams-config.webm" fw={1} fh={1} />;
export const TeamsPanelClip: React.FC<ClipP> = (p) => <Clip {...p} src="rec/teams-panel.webm" fw={1} fh={1} />;
export const TeamsStageClip: React.FC<ClipP> = (p) => <Clip {...p} src="rec/teams-stage.webm" fw={1} fh={1} />;

// ---------- Helpers ----------
/** Lays out children at a fixed design size and scales the whole box. */
export const Scaled: React.FC<{ w: number; h: number; scale: number; children: React.ReactNode; style?: React.CSSProperties }> = ({ w, h, scale, children, style }) => (
  <div style={{ width: w * scale, height: h * scale, position: 'relative', ...style }}>
    <div style={{ position: 'absolute', left: 0, top: 0, width: w, height: h, transform: `scale(${scale})`, transformOrigin: '0 0' }}>{children}</div>
  </div>
);

const useAppear = (at: number | undefined, damping = 15) => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();
  if (at === undefined) return 0;
  return spring({ frame: frame - at, fps, config: { damping, mass: 0.6 } });
};

/** A pointer that glides between key points and ripples on clicks. Points: [frame, x, y]. */
export const Pointer: React.FC<{ points: [number, number, number][]; clicks?: number[]; size?: number }> = ({ points, clicks = [], size = 30 }) => {
  const frame = useCurrentFrame();
  if (frame < points[0][0]) return null;
  const fs = points.map((p) => p[0]);
  const x = points.length > 1 ? interpolate(frame, fs, points.map((p) => p[1]), { extrapolateRight: 'clamp', easing: ease }) : points[0][1];
  const y = points.length > 1 ? interpolate(frame, fs, points.map((p) => p[2]), { extrapolateRight: 'clamp', easing: ease }) : points[0][2];
  const click = clicks.find((c) => frame >= c && frame < c + 16);
  const k = click === undefined ? 0 : (frame - click) / 16;
  return (
    <>
      {click !== undefined && (
        <div style={{
          position: 'absolute', left: x - 26, top: y - 26, width: 52, height: 52, borderRadius: '50%', zIndex: 50,
          background: 'rgba(124,77,255,.3)', border: '3px solid rgba(124,77,255,.85)', transform: `scale(${0.3 + k * 1.3})`, opacity: 1 - k,
        }} />
      )}
      <svg viewBox="0 0 24 24" width={size} height={size} style={{ position: 'absolute', left: x - size * 0.17, top: y - size * 0.08, zIndex: 51, filter: 'drop-shadow(0 2px 3px rgba(0,0,0,.4))' }}>
        <path d="M4 2 L4 20 L9 15.5 L12.5 22 L15.5 20.5 L12 14 L19 14 Z" fill="#111" stroke="#fff" strokeWidth="1.6" strokeLinejoin="round" />
      </svg>
    </>
  );
};

const Avatar: React.FC<{ text: string; color: string; size?: number; logo?: boolean }> = ({ text, color, size = 46, logo }) => (
  logo ? <div style={{ flex: 'none' }}><Logo size={size} /></div> : (
    <div style={{ flex: 'none', width: size, height: size, borderRadius: size * 0.22, background: color, color: '#fff', display: 'flex', alignItems: 'center', justifyContent: 'center', fontFamily: FONT_BODY, fontWeight: 800, fontSize: size * 0.45 }}>{text}</div>
  )
);

const Toast: React.FC<{ text: string; at?: number; style?: React.CSSProperties }> = ({ text, at, style }) => {
  const e = useAppear(at, 14);
  if (!e) return null;
  return (
    <div style={{
      position: 'absolute', display: 'flex', alignItems: 'center', gap: 12, padding: '16px 26px', borderRadius: 14, background: C.ink, color: '#fff',
      fontFamily: FONT_BODY, fontWeight: 600, fontSize: 24, boxShadow: '0 16px 40px rgba(0,0,0,.35)', opacity: e, transform: `translateY(${(1 - e) * 20}px)`, ...style,
    }}><span style={{ color: '#5fe0a0', fontSize: 26 }}>✓</span>{text}</div>
  );
};

// ---------- Chat (Slack) ----------
const ChatMessage: React.FC<{ name: string; time: string; avatar: React.ReactNode; app?: boolean; children: React.ReactNode; at?: number; highlight?: boolean; fs: number }> = ({ name, time, avatar, app, children, at, highlight, fs }) => {
  const e = at === undefined ? 1 : useAppear(at, 15);
  if (!e) return null;
  return (
    <div style={{ display: 'flex', gap: fs * 0.6, padding: `${fs * 0.5}px ${fs}px`, opacity: e, transform: `translateY(${(1 - e) * 24}px)`, background: highlight ? 'rgba(255, 207, 112, .18)' : 'transparent' }}>
      {avatar}
      <div style={{ flex: 1, minWidth: 0 }}>
        <div style={{ display: 'flex', alignItems: 'baseline', gap: fs * 0.4 }}>
          <span style={{ fontWeight: 800, fontSize: fs, color: C.ink }}>{name}</span>
          {app && <span style={{ fontSize: fs * 0.6, fontWeight: 700, color: '#6b6787', background: '#ecebf3', padding: '2px 7px', borderRadius: 4 }}>APP</span>}
          <span style={{ fontSize: fs * 0.7, color: '#8a86a0' }}>{time}</span>
        </div>
        <div style={{ fontSize: fs, color: '#2b2840', lineHeight: 1.4, marginTop: 4 }}>{children}</div>
      </div>
    </div>
  );
};

const ResultText: React.FC = () => (
  <span><b>OPP-101 Login with magic link</b>: estimate <b>5</b> (wide spread). Votes: 3, 5 ×2, 8</span>
);

/**
 * A team chat channel where someone runs /poker. Frames are local to the scene.
 * typeFrom..postAt: the command is typed; postAt: the app's message; ephemeralAt: the host link only the author sees;
 * replyAt: the result in the thread (full layout: a thread pane; compact: inline).
 */
export const ChatWindow: React.FC<{
  compact?: boolean; typeFrom?: number; postAt?: number; ephemeralAt?: number; threadAt?: number; replyAt?: number;
  joinClickAt?: number; startPosted?: boolean;
}> = ({ compact, typeFrom = 0, postAt, ephemeralAt, threadAt, replyAt, joinClickAt, startPosted }) => {
  const frame = useCurrentFrame();
  const W = compact ? 760 : 1500;
  const H = compact ? 1020 : 860;
  const fs = compact ? 30 : 25;
  const command = '/poker Sprint 24 Planning';
  const posted = startPosted || (postAt !== undefined && frame >= postAt);
  const typed = posted ? '' : command.slice(0, Math.max(0, Math.floor(interpolate(frame, [typeFrom, (postAt ?? typeFrom + 40) - 8], [0, command.length], { extrapolateLeft: 'clamp', extrapolateRight: 'clamp' }))));
  const threadOpen = !compact && threadAt !== undefined && frame >= threadAt;
  const threadE = useAppear(threadAt, 18);
  const joinPressed = joinClickAt !== undefined && frame >= joinClickAt && frame < joinClickAt + 8;

  const post = (
    <ChatMessage name="Open Planning Poker" app time="10:02" avatar={<Avatar text="" color="" logo size={fs * 1.8} />} at={startPosted ? undefined : postAt} fs={fs}>
      <span style={{ color: '#1d6fd1', background: 'rgba(29,111,209,.1)', borderRadius: 4, padding: '0 3px' }}>@Bojan</span> started planning poker: <b>Sprint 24 Planning</b>
      <div style={{ marginTop: fs * 0.5 }}>
        <span style={{
          display: 'inline-block', padding: `${fs * 0.3}px ${fs * 1.1}px`, borderRadius: 8, background: joinPressed ? '#146b46' : '#1f8a5b', color: '#fff', fontWeight: 800, fontSize: fs * 0.95,
          transform: joinPressed ? 'scale(.96)' : 'none',
        }}>Join</span>
      </div>
      {(replyAt !== undefined && frame >= replyAt) && (
        <div style={{ marginTop: fs * 0.4, fontSize: fs * 0.8, color: '#1d6fd1', fontWeight: 700 }}>1 reply</div>
      )}
    </ChatMessage>
  );

  return (
    <div style={{ width: W, height: H, display: 'flex', borderRadius: 18, overflow: 'hidden', background: '#fff', fontFamily: FONT_BODY, boxShadow: '0 40px 100px rgba(10,5,40,.45)' }}>
      {!compact && (
        <div style={{ width: 280, background: C.navy2, color: '#d8d0f5', padding: '26px 0', fontSize: 22, flex: 'none' }}>
          <div style={{ padding: '0 24px 22px', fontFamily: FONT_DISPLAY, fontWeight: 800, fontSize: 26, color: '#fff' }}>Acme Engineering</div>
          {['general', 'sprint-planning', 'backend', 'random'].map((c) => (
            <div key={c} style={{ padding: '8px 24px', background: c === 'sprint-planning' ? C.primary : 'transparent', color: c === 'sprint-planning' ? '#fff' : undefined, fontWeight: c === 'sprint-planning' ? 700 : 500 }}># {c}</div>
          ))}
          <div style={{ padding: '22px 24px 8px', fontSize: 18, opacity: 0.7 }}>Apps</div>
          <div style={{ padding: '8px 24px', display: 'flex', alignItems: 'center', gap: 10 }}><Logo size={26} /> Open Planning Poker</div>
        </div>
      )}
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0 }}>
        <div style={{ height: fs * 2.6, borderBottom: '2px solid #ecebf3', display: 'flex', alignItems: 'center', padding: `0 ${fs}px`, fontWeight: 800, fontSize: fs * 1.05, color: C.ink, flex: 'none' }}>
          # sprint-planning <span style={{ marginLeft: 'auto', fontSize: fs * 0.75, fontWeight: 600, color: '#8a86a0' }}>👥 8</span>
        </div>
        <div style={{ flex: 1, display: 'flex', flexDirection: 'column', justifyContent: 'flex-end', paddingBottom: fs * 0.5, overflow: 'hidden' }}>
          <ChatMessage name="Marko" time="9:58" avatar={<Avatar text="M" color="#2e9e8f" size={fs * 1.8} />} fs={fs}>Planning in 5 minutes, grab a coffee ☕</ChatMessage>
          <ChatMessage name="Jelena" time="9:59" avatar={<Avatar text="J" color="#d9822b" size={fs * 1.8} />} fs={fs}>Backlog is groomed, 3 stories ready 👍</ChatMessage>
          {post}
          {ephemeralAt !== undefined && (
            <ChatMessage name="Open Planning Poker" app time="10:02" avatar={<Avatar text="" color="" logo size={fs * 1.8} />} at={ephemeralAt} fs={fs}>
              <div style={{ fontSize: fs * 0.72, color: '#6b6787', marginBottom: 4 }}>👁 Only visible to you</div>
              Your game is ready. <span style={{ color: '#1d6fd1', fontWeight: 700, textDecoration: 'underline' }}>Open as host</span> (only you see this link; the first person to open it becomes the host). Results are posted in the thread after each reveal.
            </ChatMessage>
          )}
          {compact && replyAt !== undefined && (
            <div style={{ marginLeft: fs * 2.4, borderLeft: '4px solid #e3e1ec' }}>
              <ChatMessage name="Open Planning Poker" app time="10:09" avatar={<Avatar text="" color="" logo size={fs * 1.5} />} at={replyAt} highlight fs={fs}><ResultText /></ChatMessage>
            </div>
          )}
        </div>
        <div style={{ margin: `0 ${fs}px ${fs}px`, border: '2px solid #d9d6e6', borderRadius: 12, padding: `${fs * 0.6}px ${fs * 0.7}px`, fontSize: fs, color: typed ? C.ink : '#a19db8', fontFamily: typed ? FONT_MONO : FONT_BODY, flex: 'none', minHeight: fs * 1.4 }}>
          {typed || 'Message #sprint-planning'}{typed && frame % 30 < 16 ? <span style={{ color: C.primary }}>▍</span> : null}
          {typed.startsWith('/') && (
            <div style={{ marginTop: fs * 0.4, fontFamily: FONT_BODY, fontSize: fs * 0.7, color: '#6b6787' }}>/poker [game name] · Start a planning poker game in this channel · Open Planning Poker</div>
          )}
        </div>
      </div>
      {threadOpen && (
        <div style={{ width: 520, borderLeft: '2px solid #ecebf3', display: 'flex', flexDirection: 'column', flex: 'none', opacity: threadE, transform: `translateX(${(1 - threadE) * 80}px)` }}>
          <div style={{ height: fs * 2.6, borderBottom: '2px solid #ecebf3', display: 'flex', alignItems: 'center', padding: `0 ${fs}px`, fontWeight: 800, fontSize: fs * 1.05, color: C.ink }}>
            Thread <span style={{ marginLeft: 10, fontWeight: 500, fontSize: fs * 0.8, color: '#8a86a0' }}># sprint-planning</span>
          </div>
          <div style={{ paddingTop: fs * 0.6 }}>
            <ChatMessage name="Open Planning Poker" app time="10:02" avatar={<Avatar text="" color="" logo size={fs * 1.8} />} fs={fs}>
              <span style={{ color: '#1d6fd1' }}>@Bojan</span> started planning poker: <b>Sprint 24 Planning</b>
            </ChatMessage>
            <div style={{ margin: `${fs * 0.4}px ${fs}px`, fontSize: fs * 0.75, color: '#8a86a0', display: 'flex', alignItems: 'center', gap: 10 }}>
              {replyAt !== undefined && frame >= replyAt ? '1 reply' : 'No replies yet'}<div style={{ flex: 1, height: 2, background: '#ecebf3' }} />
            </div>
            {replyAt !== undefined && (
              <ChatMessage name="Open Planning Poker" app time="10:09" avatar={<Avatar text="" color="" logo size={fs * 1.8} />} at={replyAt} highlight fs={fs}><ResultText /></ChatMessage>
            )}
          </div>
        </div>
      )}
    </div>
  );
};

/** One thread reply on its own (vertical video). Design width 900. */
export const ThreadReplyCard: React.FC<{ at: number }> = ({ at }) => {
  const e = useAppear(at, 14);
  return (
    <div style={{ width: 900, background: '#fff', borderRadius: 18, padding: '18px 0', fontFamily: FONT_BODY, boxShadow: '0 30px 80px rgba(10,5,40,.45)', opacity: e, transform: `translateY(${(1 - e) * 40}px)` }}>
      <div style={{ padding: '0 30px 8px', fontSize: 22, color: '#8a86a0', fontWeight: 700 }}># sprint-planning · thread</div>
      <ChatMessage name="Open Planning Poker" app time="10:09" avatar={<Avatar text="" color="" logo size={54} />} fs={30}><ResultText /></ChatMessage>
    </div>
  );
};

// ---------- Jira ----------
const ISSUES = [
  ['OPP-101', 'Login with magic link'],
  ['OPP-102', 'Dark mode toggle'],
  ['OPP-103', 'Export sprint report'],
];
const Field: React.FC<{ label: string; value: string; w?: number }> = ({ label, value, w = 300 }) => (
  <div style={{ width: w, border: '2px solid #d9d6e6', borderRadius: 12, padding: '10px 16px', position: 'relative' }}>
    <div style={{ fontSize: 16, color: '#6b6787' }}>{label}</div>
    <div style={{ fontSize: 22, fontWeight: 600, color: C.ink, display: 'flex', justifyContent: 'space-between' }}>{value}<span style={{ color: C.primary }}>▾</span></div>
  </div>
);

/** The app's Import dialog, "From Jira" tab (labels from en.ts). Design size 900 × 720. */
export const JiraImport: React.FC<{ listAt: number; clickAt: number; toastAt?: number }> = ({ listAt, clickAt, toastAt }) => {
  const frame = useCurrentFrame();
  const pressed = frame >= clickAt && frame < clickAt + 8;
  return (
    <div style={{ width: 900, height: 720, position: 'relative', background: '#fff', borderRadius: 24, padding: '34px 40px', boxSizing: 'border-box', fontFamily: FONT_BODY, boxShadow: '0 40px 100px rgba(10,5,40,.45)' }}>
      <div style={{ fontFamily: FONT_DISPLAY, fontWeight: 800, fontSize: 36, color: C.ink }}>Import tickets</div>
      <div style={{ display: 'flex', gap: 34, marginTop: 20, borderBottom: '2px solid #ecebf3', fontSize: 22, fontWeight: 700 }}>
        <div style={{ paddingBottom: 12, color: '#8a86a0' }}>Paste</div>
        <div style={{ paddingBottom: 10, color: C.primary, borderBottom: `4px solid ${C.primary}` }}>From Jira</div>
      </div>
      <div style={{ marginTop: 20, display: 'flex', alignItems: 'center', gap: 10, fontSize: 20, color: '#3c3856' }}>
        <span style={{ width: 12, height: 12, borderRadius: '50%', background: '#2bb673' }} /> Connected to acme.atlassian.net
      </div>
      <div style={{ display: 'flex', gap: 18, marginTop: 20 }}>
        <Field label="Board" value="OPP board" />
        <Field label="Sprint" value="Sprint 24 (active)" w={340} />
      </div>
      <div style={{ marginTop: 22, border: '2px solid #ecebf3', borderRadius: 14, overflow: 'hidden' }}>
        {ISSUES.map(([k, t], i) => {
          const e = useAppear(listAt + i * 5, 16);
          return (
            <div key={k} style={{ display: 'flex', alignItems: 'center', gap: 16, padding: '15px 18px', borderTop: i ? '2px solid #f1f0f6' : 'none', fontSize: 22, opacity: e, transform: `translateX(${(1 - e) * 30}px)` }}>
              <span style={{ width: 26, height: 26, borderRadius: 6, background: C.primary, color: '#fff', display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 18, fontWeight: 900 }}>✓</span>
              <span style={{ width: 18, height: 18, borderRadius: 4, background: '#36b37e' }} />
              <span style={{ fontFamily: FONT_MONO, fontWeight: 700, color: C.primary }}>{k}</span>
              <span style={{ color: C.ink }}>{t}</span>
            </div>
          );
        })}
      </div>
      <div style={{ position: 'absolute', right: 40, bottom: 34, display: 'flex', gap: 14 }}>
        <span style={{ padding: '14px 26px', borderRadius: 999, fontSize: 22, fontWeight: 700, color: C.primary }}>Cancel</span>
        <span style={{ padding: '14px 30px', borderRadius: 999, background: pressed ? '#5228c4' : C.primary, color: '#fff', fontSize: 22, fontWeight: 800, transform: pressed ? 'scale(.96)' : 'none' }}>Import 3</span>
      </div>
      {toastAt !== undefined && <Toast text="Imported 3 issues from Jira." at={toastAt} style={{ left: 40, bottom: -90 }} />}
    </div>
  );
};

/** An issue in the tracker whose story points fill in when the estimate is saved. Design size 760 × 560. */
export const JiraIssueCard: React.FC<{ savedAt: number }> = ({ savedAt }) => {
  const frame = useCurrentFrame();
  const saved = frame >= savedAt;
  const glow = interpolate(frame, [savedAt, savedAt + 10, savedAt + 50], [0, 1, 0.35], { extrapolateLeft: 'clamp', extrapolateRight: 'clamp' });
  const pop = useAppear(savedAt, 10);
  const row = (k: string, v: React.ReactNode, hl = false) => (
    <div style={{ display: 'flex', alignItems: 'center', padding: '14px 18px', borderRadius: 10, fontSize: 22, background: hl ? `rgba(106,58,232,${0.12 * glow})` : 'transparent', boxShadow: hl ? `0 0 0 ${3 * glow}px rgba(106,58,232,.6)` : 'none' }}>
      <span style={{ width: 200, color: '#6b6787' }}>{k}</span><span style={{ color: C.ink, fontWeight: 600 }}>{v}</span>
    </div>
  );
  return (
    <div style={{ width: 760, height: 560, background: '#fff', borderRadius: 18, padding: '30px 34px', boxSizing: 'border-box', fontFamily: FONT_BODY, boxShadow: '0 40px 100px rgba(10,5,40,.45)' }}>
      <div style={{ fontSize: 18, color: '#6b6787' }}>acme.atlassian.net · Projects / OPP / <b style={{ color: '#2f6fd6' }}>OPP-101</b></div>
      <div style={{ marginTop: 14, fontFamily: FONT_DISPLAY, fontWeight: 800, fontSize: 38, color: C.ink }}>Login with magic link</div>
      <div style={{ marginTop: 14, display: 'inline-block', padding: '6px 14px', borderRadius: 6, background: '#e9f0fb', color: '#2f6fd6', fontWeight: 800, fontSize: 18 }}>TO DO</div>
      <div style={{ marginTop: 26, borderTop: '2px solid #f1f0f6', paddingTop: 12 }}>
        {row('Assignee', 'Marko')}
        {row('Sprint', 'Sprint 24')}
        {row('Story points', saved
          ? <span style={{ display: 'inline-flex', alignItems: 'center', justifyContent: 'center', minWidth: 48, height: 40, borderRadius: 20, background: C.primary, color: '#fff', fontWeight: 800, transform: `scale(${0.6 + pop * 0.4})` }}>5</span>
          : <span style={{ color: '#a19db8', fontWeight: 500 }}>None</span>, true)}
        {row('Labels', 'auth')}
      </div>
    </div>
  );
};

// ---------- Teams meeting ----------
const PEOPLE: [string, string][] = [['Bojan', '#6a3ae8'], ['Ana', '#d24b8f'], ['Marko', '#2e9e8f'], ['Jelena', '#d9822b']];
const Tile: React.FC<{ name: string; color: string; fs?: number }> = ({ name, color, fs = 22 }) => (
  <div style={{ background: '#2b2a3d', borderRadius: 14, display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', position: 'relative', minHeight: 0 }}>
    <div style={{ width: fs * 4, height: fs * 4, borderRadius: '50%', background: color, color: '#fff', display: 'flex', alignItems: 'center', justifyContent: 'center', fontFamily: FONT_BODY, fontWeight: 800, fontSize: fs * 1.7 }}>{name[0]}</div>
    <div style={{ position: 'absolute', left: 14, bottom: 10, fontFamily: FONT_BODY, fontSize: fs * 0.8, color: '#e8e6f4', fontWeight: 600 }}>{name}</div>
  </div>
);

const MeetingBar: React.FC<{ w: number; title: string }> = ({ w, title }) => (
  <div style={{ height: 64, background: '#1b1a29', display: 'flex', alignItems: 'center', padding: '0 22px', gap: 20, fontFamily: FONT_BODY, color: '#e8e6f4', width: w, boxSizing: 'border-box' }}>
    <span style={{ fontWeight: 800, fontSize: 22 }}>{title}</span>
    <span style={{ fontFamily: FONT_MONO, fontSize: 18, color: '#a7a3c4' }}>12:41</span>
    <div style={{ marginLeft: 'auto', display: 'flex', gap: 10, alignItems: 'center' }}>
      {['💬 Chat', '👥 People'].map((t) => <span key={t} style={{ padding: '8px 14px', borderRadius: 8, fontSize: 18, color: '#c9c5e3' }}>{t}</span>)}
      <span style={{ padding: '8px 14px', borderRadius: 8, fontSize: 18, background: '#34325a', display: 'flex', alignItems: 'center', gap: 8 }}><Logo size={22} /> Open Planning Poker</span>
      <span style={{ padding: '8px 14px', borderRadius: 8, fontSize: 18, color: '#c9c5e3' }}>🎙 📷 ⤴</span>
      <span style={{ padding: '8px 18px', borderRadius: 8, fontSize: 18, fontWeight: 800, background: '#c4314b', color: '#fff' }}>Leave</span>
    </div>
  </div>
);

/** A video meeting with the app in the side panel; stage shows the participants or the app (real clips). 1600 × 900. */
export const TeamsMeeting: React.FC<{ panelFromMs: number; rate?: number; stage: 'tiles' | 'app'; dim?: boolean; panel?: boolean; children?: React.ReactNode }> = ({ panelFromMs, rate = 1, stage, dim, panel = true, children }) => (
  <div style={{ width: 1600, height: 900, borderRadius: 18, overflow: 'hidden', background: '#11101c', position: 'relative', boxShadow: '0 40px 100px rgba(10,5,40,.5)' }}>
    <MeetingBar w={1600} title="Sprint 25 Planning" />
    <div style={{ position: 'absolute', left: 16, top: 80, width: panel ? 1184 : 1568, height: 804 }}>
      {stage === 'tiles' ? (
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gridTemplateRows: '1fr 1fr', gap: 12, width: '100%', height: '100%' }}>
          {PEOPLE.map(([n, c]) => <Tile key={n} name={n} color={c} fs={26} />)}
        </div>
      ) : (
        <div style={{ position: 'relative', width: '100%', height: '100%' }}>
          <div style={{ position: 'absolute', left: 0, top: 0, width: 1184, height: 666, borderRadius: 12, overflow: 'hidden', background: C.bg }}>
            <TeamsStageClip fromMs={panelFromMs - TM.stageOffset} rate={rate} />
          </div>
          <div style={{ position: 'absolute', left: 0, top: 680, width: 1184, height: 124, display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 12 }}>
            {PEOPLE.map(([n, c]) => <Tile key={n} name={n} color={c} fs={14} />)}
          </div>
        </div>
      )}
    </div>
    {panel && <div style={{ position: 'absolute', left: 1216, top: 80, width: 368, height: 804, borderRadius: 12, overflow: 'hidden', background: '#fff' }}>
      <div style={{ height: 44, display: 'flex', alignItems: 'center', gap: 10, padding: '0 14px', fontFamily: FONT_BODY, fontWeight: 800, fontSize: 18, color: C.ink, borderBottom: '1px solid #ecebf3' }}>
        <Logo size={24} /> Open Planning Poker <span style={{ marginLeft: 'auto', color: '#8a86a0' }}>✕</span>
      </div>
      <div style={{ position: 'relative', width: 360, height: 760, marginLeft: 4 }}><TeamsPanelClip fromMs={panelFromMs} rate={rate} /></div>
    </div>}
    {dim && <div style={{ position: 'absolute', inset: 0, background: 'rgba(8,6,20,.62)' }} />}
    {children}
  </div>
);

/** Teams' "add a tab" dialog around the real setup page. Dialog design size 720 × 700, placed inside TeamsMeeting. */
export const TeamsConfigDialog: React.FC<{ fromMs: number; rate: number; saveAt: number; style?: React.CSSProperties }> = ({ fromMs, rate, saveAt, style }) => {
  const frame = useCurrentFrame();
  const pressed = frame >= saveAt && frame < saveAt + 8;
  return (
    <div style={{ position: 'absolute', width: 720, height: 700, background: '#fff', borderRadius: 14, overflow: 'hidden', boxShadow: '0 30px 90px rgba(0,0,0,.55)', fontFamily: FONT_BODY, ...style }}>
      <div style={{ height: 70, display: 'flex', alignItems: 'center', gap: 14, padding: '0 26px', borderBottom: '1px solid #ecebf3' }}>
        <Logo size={36} /><span style={{ fontWeight: 800, fontSize: 24, color: C.ink }}>Open Planning Poker</span>
        <span style={{ marginLeft: 'auto', fontSize: 22, color: '#8a86a0' }}>✕</span>
      </div>
      <div style={{ position: 'absolute', left: 40, top: 80, width: 640, height: 520, overflow: 'hidden' }}><TeamsConfigClip fromMs={fromMs} rate={rate} /></div>
      <div style={{ position: 'absolute', right: 26, bottom: 22, display: 'flex', gap: 12 }}>
        <span style={{ padding: '12px 26px', borderRadius: 8, border: '2px solid #d9d6e6', fontWeight: 700, fontSize: 20, color: C.ink }}>Back</span>
        <span style={{ padding: '12px 30px', borderRadius: 8, background: pressed ? '#4447a8' : '#5b5fc7', color: '#fff', fontWeight: 800, fontSize: 20, transform: pressed ? 'scale(.96)' : 'none' }}>Save</span>
      </div>
    </div>
  );
};
