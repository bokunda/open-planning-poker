import { isPlatformBrowser } from '@angular/common';
import { Injectable, InjectionToken, PLATFORM_ID, inject, DOCUMENT } from '@angular/core';
import { Translation, TranslocoLoader, TranslocoService } from '@jsverse/transloco';
import { firstValueFrom } from 'rxjs';

/** Languages of the app (OPP-24). Serbian is written in Latin script. */
export const LANGUAGES = [
  { code: 'en', label: 'English', htmlLang: 'en' },
  { code: 'sr', label: 'Srpski', htmlLang: 'sr-Latn' },
  { code: 'de', label: 'Deutsch', htmlLang: 'de' },
] as const;

export type Language = (typeof LANGUAGES)[number]['code'];
export const DEFAULT_LANGUAGE: Language = 'en';
export const LANGUAGE_COOKIE = 'opp_lang';

/** On the server: the language chosen in server.ts from the cookie or Accept-Language. */
export const SERVER_LANGUAGE = new InjectionToken<Language>('SERVER_LANGUAGE');

export function isLanguage(value: unknown): value is Language {
  return LANGUAGES.some((language) => language.code === value);
}

/** Maps `sr-Latn-RS`, `de-AT`, `en-GB`... to a supported language, or null. */
export function toLanguage(tag: string | null | undefined): Language | null {
  const base = tag?.trim().toLowerCase().split(/[-_]/)[0];
  if (!base) return null;
  if (isLanguage(base)) return base;
  // Serbo-Croatian variants read Serbian Latin without trouble.
  return ['hr', 'bs', 'sh', 'cnr'].includes(base) ? 'sr' : null;
}

/** The language for a request: the remembered choice first, then the browser's preferred languages. */
export function negotiateLanguage(cookieHeader: string | undefined, acceptLanguage: string | undefined): Language {
  const cookie = cookieHeader?.split(';').map((part) => part.trim().split('='))
    .find(([name]) => name === LANGUAGE_COOKIE)?.[1];
  if (isLanguage(cookie)) return cookie;

  const preferred = (acceptLanguage ?? '').split(',')
    .map((part) => {
      const [tag, q] = part.trim().split(';q=');
      return { tag, q: q ? Number(q) : 1 };
    })
    .sort((a, b) => b.q - a.q);
  for (const { tag } of preferred) {
    const language = toLanguage(tag);
    if (language) return language;
  }
  return DEFAULT_LANGUAGE;
}

/** Each language is its own chunk, loaded on demand (English is loaded with the app). */
@Injectable({ providedIn: 'root' })
export class AppTranslationLoader implements TranslocoLoader {
  getTranslation(lang: string): Promise<Translation> {
    switch (lang) {
      case 'sr': return import('./sr').then((m) => m.default);
      case 'de': return import('./de').then((m) => m.default);
      default: return import('./en').then((m) => m.default);
    }
  }
}

/**
 * Active language: chosen on the server (cookie or Accept-Language) and written to `<html lang>`; the browser
 * starts with the same language, so hydration matches. A change is remembered in a cookie for a year.
 */
@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly transloco = inject(TranslocoService);
  private readonly document = inject(DOCUMENT);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
  private readonly serverLanguage = inject(SERVER_LANGUAGE, { optional: true });

  get current(): Language {
    const active = this.transloco.getActiveLang();
    return isLanguage(active) ? active : DEFAULT_LANGUAGE;
  }

  /** Runs before the first render (APP_INITIALIZER). */
  async init(): Promise<void> {
    await this.use(this.initialLanguage(), false);
  }

  async use(language: Language, remember = true): Promise<void> {
    await firstValueFrom(this.transloco.load(language));
    this.transloco.setActiveLang(language);
    this.document.documentElement.lang = LANGUAGES.find((l) => l.code === language)!.htmlLang;
    this.document.documentElement.setAttribute('data-opp-lang', language);
    if (remember && this.isBrowser) {
      this.document.cookie = `${LANGUAGE_COOKIE}=${language}; Path=/; Max-Age=31536000; SameSite=Lax`;
    }
  }

  private initialLanguage(): Language {
    if (!this.isBrowser) return this.serverLanguage ?? DEFAULT_LANGUAGE;
    // The server-rendered page says which language it used; a client-only page falls back to the cookie
    // and the browser languages, like the server does.
    const rendered = toLanguage(this.document.documentElement.getAttribute('data-opp-lang'));
    if (rendered) return rendered;
    return negotiateLanguage(this.document.cookie, navigator.languages?.join(',') ?? navigator.language);
  }
}
