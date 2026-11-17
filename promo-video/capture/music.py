# Generates a soft, royalty-free music bed (pads + plucked arpeggio + light beat). Output: public/music.wav
import numpy as np, wave, sys
SR = 44100; BPM = 96; beat = 60 / BPM; bar = beat * 4
DUR = float(sys.argv[1]) if len(sys.argv) > 1 else 100
n = int(SR * DUR); t = np.arange(n) / SR
out = np.zeros((n, 2))
def note(m): return 440 * 2 ** ((m - 69) / 12)
prog = [[60, 64, 67, 71], [57, 60, 64, 67], [53, 57, 60, 64], [55, 59, 62, 67]]  # Cmaj7 Am7 Fmaj7 G
bass = [36, 33, 29, 31]
def env(length, a, r):
    L = int(length * SR); e = np.ones(L)
    A = int(a * SR); R = int(r * SR)
    e[:A] = np.linspace(0, 1, A); e[-R:] *= np.linspace(1, 0, R); return e
def add(sig, start, gain, pan=0.0):
    s = int(start * SR)
    if s >= n: return
    sig = sig[: n - s]
    out[s:s + len(sig), 0] += sig * gain * (1 - pan) ; out[s:s + len(sig), 1] += sig * gain * (1 + pan)
rng = np.random.default_rng(7)
bars = int(DUR / bar) + 1
for b in range(bars):
    ch = prog[b % 4]; t0 = b * bar
    L = bar + 0.6; tt = np.arange(int(L * SR)) / SR
    pad = sum(np.sin(2*np.pi*note(m)*tt) + 0.5*np.sin(2*np.pi*note(m)*1.003*tt) + 0.25*np.sin(2*np.pi*note(m+12)*tt) for m in ch)
    add(pad * env(L, 0.8, 0.9) / len(ch), t0, 0.05)
    bl = bar; tb = np.arange(int(bl * SR)) / SR
    add(np.sin(2*np.pi*note(bass[b % 4])*tb) * env(bl, 0.02, 0.4), t0, 0.12)
    arp = [ch[0]+12, ch[1]+12, ch[2]+12, ch[3]+12, ch[2]+12, ch[1]+12, ch[0]+24, ch[2]+12]
    for i, m in enumerate(arp):
        pl = 0.9; tp = np.arange(int(pl * SR)) / SR
        sig = (np.sin(2*np.pi*note(m)*tp) + 0.3*np.sin(2*np.pi*note(m)*2*tp)) * np.exp(-tp * 5)
        add(sig, t0 + i * beat / 2, 0.045, pan=0.35 if i % 2 else -0.35)
    if b >= 1:
        for k in range(4):
            tk = np.arange(int(0.35 * SR)) / SR
            kick = np.sin(2*np.pi*(50 + 90*np.exp(-tk*30))*tk) * np.exp(-tk * 9)
            if k in (0, 2): add(kick, t0 + k * beat, 0.22)
            hat = rng.standard_normal(int(0.05 * SR)) * np.exp(-np.arange(int(0.05*SR))/SR*90)
            hat = np.diff(hat, prepend=0)
            add(hat, t0 + k * beat + beat / 2, 0.035)
            if k in (1, 3):
                sn = rng.standard_normal(int(0.18*SR)) * np.exp(-np.arange(int(0.18*SR))/SR*25)
                add(np.diff(sn, prepend=0), t0 + k * beat, 0.05)
# simple stereo delay/reverb-ish smear
d = int(0.28 * SR); wet = np.zeros_like(out); wet[d:] = out[:-d] * 0.25; out += wet[:, ::-1]
out /= np.max(np.abs(out)) * 1.12
fade = int(2 * SR); out[:fade] *= np.linspace(0, 1, fade)[:, None]; out[-fade:] *= np.linspace(1, 0, fade)[:, None]
pcm = (out * 32767).astype('<i2')
with wave.open('public/music.wav', 'wb') as w:
    w.setnchannels(2); w.setsampwidth(2); w.setframerate(SR); w.writeframes(pcm.tobytes())
print('ok', DUR)
