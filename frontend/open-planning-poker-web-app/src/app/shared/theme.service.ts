import { Injectable, Inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { BehaviorSubject } from 'rxjs';

export type ThemeMode = 'light' | 'dark';

const THEME_KEY = 'opp-theme';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly _current = new BehaviorSubject<ThemeMode>('light');
  private readonly isBrowser: boolean;
  readonly current$ = this._current.asObservable();

  get current(): ThemeMode { return this._current.value; }
  get isDark(): boolean { return this._current.value === 'dark'; }

  constructor(@Inject(PLATFORM_ID) platformId: object) {
    this.isBrowser = isPlatformBrowser(platformId);
    if (this.isBrowser) {
      const saved = localStorage.getItem(THEME_KEY) as ThemeMode | null;
      const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
      const initial: ThemeMode = saved ?? (prefersDark ? 'dark' : 'light');
      this.apply(initial);
    }
  }

  toggle(): void {
    this.apply(this._current.value === 'dark' ? 'light' : 'dark');
  }

  set(mode: ThemeMode): void {
    this.apply(mode);
  }

  private apply(mode: ThemeMode): void {
    this._current.next(mode);
    if (this.isBrowser) {
      document.documentElement.classList.toggle('dark-theme', mode === 'dark');
      localStorage.setItem(THEME_KEY, mode);
    }
  }
}
