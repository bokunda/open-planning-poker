import { isConsensus } from '../home/game/facilitation/celebration.component';
import { isWinterSeason } from '../home/game/room-types';
import { escapeHtml, setMetaContent, withInvitePreview } from '../../shared/link-preview';

describe('consensus celebration', () => {
  it('needs two or more players with the same sizing card', () => {
    expect(isConsensus(['5', '5', '5'])).toBe(true);
    expect(isConsensus(['5'])).toBe(false);
    expect(isConsensus(['5', '8'])).toBe(false);
    expect(isConsensus(['5', ''])).toBe(false);
    expect(isConsensus(['❓', '❓'])).toBe(false);
    expect(isConsensus(['⏸️', '⏸️'])).toBe(false);
    expect(isConsensus(['M', 'M'])).toBe(true);
  });
});

describe('seasonal card backs', () => {
  it('are on from 1 December to 6 January', () => {
    expect(isWinterSeason(new Date(2026, 11, 1))).toBe(true);
    expect(isWinterSeason(new Date(2027, 0, 6))).toBe(true);
    expect(isWinterSeason(new Date(2027, 0, 7))).toBe(false);
    expect(isWinterSeason(new Date(2026, 10, 30))).toBe(false);
  });
});

describe('invite link preview', () => {
  const html = '<head><title>Open Planning Poker</title>'
    + '<meta name="description" content="Default">'
    + '<meta property="og:title" content="Default">'
    + '<meta property="og:description" content="Default">'
    + '<meta name="twitter:title" content="Default">'
    + '<meta name="twitter:description" content="Default"></head>';

  it('escapes the room name', () => {
    expect(escapeHtml('<b>"A & B"</b>')).toBe('&lt;b&gt;&quot;A &amp; B&quot;&lt;/b&gt;');
  });

  it('only changes the named tag', () => {
    const out = setMetaContent(html, 'og:title', 'New');
    expect(out).toContain('<meta property="og:title" content="New">');
    expect(out).toContain('<meta name="twitter:title" content="Default">');
  });

  it('puts the room name into the title and the preview tags', () => {
    const out = withInvitePreview(html, 'Sprint <7>', 'en');
    expect(out).toContain('<title>Join “Sprint &lt;7&gt;” — planning poker</title>');
    expect(out).toContain('<meta property="og:title" content="Join “Sprint &lt;7&gt;” — planning poker">');
    expect(out).toContain('<meta name="twitter:title" content="Join “Sprint &lt;7&gt;” — planning poker">');
    expect(out).not.toContain('content="Default"');
  });

  it('uses the visitor language', () => {
    expect(withInvitePreview(html, 'Sprint', 'sr')).toContain('Pridruži se');
    expect(withInvitePreview(html, 'Sprint', 'de')).toContain('beitreten');
  });
});
