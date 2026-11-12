import { Directive, HostListener, Input } from '@angular/core';

type Gtag = (command: 'event', name: string, params?: Record<string, unknown>) => void;

/**
 * Sends a GA4 event when analytics is loaded (index.html defines `gtag`). Does nothing on the
 * server, with an ad blocker, or on a self-hosted install without analytics. Never send personal data.
 */
export function trackEvent(name: string, params: Record<string, string | number> = {}): void {
  if (typeof window === 'undefined') return;
  const gtag = (window as unknown as { gtag?: Gtag }).gtag;
  if (typeof gtag !== 'function') return;
  // Beacon: the event still goes out when the link opens a new tab or leaves the page.
  gtag('event', name, { ...params, transport_type: 'beacon' });
}

export type DonationPlatform = 'ko_fi' | 'github_sponsors';

/**
 * Counts clicks on a donation link as a `donate_click` GA4 event with the platform and where the
 * link is (`header_menu`, `footer`, `about`). Register `platform` and `placement` as event-scoped
 * custom dimensions in GA4 to report on them.
 */
@Directive({ selector: 'a[oppDonate]', standalone: true })
export class DonateClickDirective {
  @Input({ required: true }) oppDonate!: DonationPlatform;
  @Input({ required: true }) oppDonatePlacement!: string;

  @HostListener('click')
  onClick(): void {
    trackEvent('donate_click', { platform: this.oppDonate, placement: this.oppDonatePlacement });
  }
}
