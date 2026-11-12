import { DOCUMENT } from '@angular/common';
import { Injectable, inject } from '@angular/core';
import { Meta } from '@angular/platform-browser';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';

const ORIGIN = 'https://app.openplanningpoker.com';
const DEFAULT_DESCRIPTION = 'Free online planning poker for agile teams. Create a room, share the link, vote in secret and reveal the cards together. No sign-up, works on phones, open source.';

interface PageSeo {
  /** Indexed pages get a canonical URL; game rooms are private and are not indexed. */
  index: boolean;
  canonicalPath?: string;
  description?: string;
}

/** SEO tags per route. Runs during server rendering too, so crawlers get them in the HTML. */
function seoFor(path: string): PageSeo {
  if (path === '/' || path === '') return { index: true, canonicalPath: '/', description: DEFAULT_DESCRIPTION };
  // "/game" shows the same page as "/", so it points search engines to "/".
  if (path === '/game') return { index: true, canonicalPath: '/', description: DEFAULT_DESCRIPTION };
  // Game rooms and tickets are private, short-lived and need a link: keep them out of search results.
  if (path.startsWith('/game/')) return { index: false, description: 'A planning poker room on Open Planning Poker. Join with the link from your host.' };
  // Team rooms are private too; "/team" only lists the visitor's own teams.
  if (path === '/team' || path.startsWith('/team/')) return { index: false, description: 'A team room on Open Planning Poker: sprint history and velocity of one team.' };
  return { index: false };
}

@Injectable({ providedIn: 'root' })
export class SeoService {
  private readonly meta = inject(Meta);
  private readonly document = inject(DOCUMENT);
  private readonly router = inject(Router);

  init(): void {
    this.router.events
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
      .subscribe(e => this.apply(e.urlAfterRedirects.split(/[?#]/)[0]));
  }

  private apply(path: string): void {
    const seo = seoFor(path);
    this.meta.updateTag({
      name: 'robots',
      content: seo.index ? 'index, follow, max-image-preview:large, max-snippet:-1, max-video-preview:-1' : 'noindex, nofollow',
    });
    if (seo.description) {
      this.meta.updateTag({ name: 'description', content: seo.description });
      this.meta.updateTag({ property: 'og:description', content: seo.description });
    }
    const url = seo.canonicalPath ? ORIGIN + seo.canonicalPath : null;
    this.setCanonical(url);
    this.meta.updateTag({ property: 'og:url', content: url ?? ORIGIN + path });
  }

  private setCanonical(url: string | null): void {
    let link = this.document.head.querySelector<HTMLLinkElement>('link[rel="canonical"]');
    if (!url) { link?.remove(); return; }
    if (!link) {
      link = this.document.createElement('link');
      link.setAttribute('rel', 'canonical');
      this.document.head.appendChild(link);
    }
    link.setAttribute('href', url);
  }
}
