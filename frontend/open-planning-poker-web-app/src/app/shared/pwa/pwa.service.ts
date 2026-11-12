import { DestroyRef, Injectable, PLATFORM_ID, inject, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { MatSnackBar } from '@angular/material/snack-bar';
import { SwUpdate, VersionReadyEvent } from '@angular/service-worker';
import { filter } from 'rxjs';

/** The browser's install event (not in the TypeScript DOM types yet). */
interface BeforeInstallPromptEvent extends Event {
  prompt(): Promise<void>;
  userChoice: Promise<{ outcome: 'accepted' | 'dismissed' }>;
}

const INSTALL_PROMPT_KEY = 'opp.installPromptShownAt';
const INSTALL_PROMPT_INTERVAL_MS = 7 * 24 * 60 * 60 * 1000;

/**
 * Installable app (OPP-25): connection state for the offline notice, the "Install app" offer (shown at most
 * once a week) and a reload offer when a new version of the app is ready. Does nothing on the server.
 */
@Injectable({ providedIn: 'root' })
export class PwaService {
  private readonly snackBar = inject(MatSnackBar);
  private readonly swUpdate = inject(SwUpdate);
  private readonly destroyRef = inject(DestroyRef);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  private installEvent: BeforeInstallPromptEvent | null = null;

  /** False while the browser has no network connection. */
  readonly online = signal(true);
  /** True while the browser offers to install the app (the header menu shows "Install app"). */
  readonly canInstall = signal(false);

  init(): void {
    if (!this.isBrowser) return;

    this.online.set(navigator.onLine);
    const onOnline = () => this.online.set(true);
    const onOffline = () => this.online.set(false);
    const onInstallPrompt = (event: Event) => {
      // Keep the browser's mini-infobar away; the app offers installing itself, at most once a week.
      event.preventDefault();
      this.installEvent = event as BeforeInstallPromptEvent;
      this.canInstall.set(true);
      this.offerInstall();
    };
    const onInstalled = () => {
      this.installEvent = null;
      this.canInstall.set(false);
    };
    window.addEventListener('online', onOnline);
    window.addEventListener('offline', onOffline);
    window.addEventListener('beforeinstallprompt', onInstallPrompt);
    window.addEventListener('appinstalled', onInstalled);
    this.destroyRef.onDestroy(() => {
      window.removeEventListener('online', onOnline);
      window.removeEventListener('offline', onOffline);
      window.removeEventListener('beforeinstallprompt', onInstallPrompt);
      window.removeEventListener('appinstalled', onInstalled);
    });

    if (this.swUpdate.isEnabled) {
      this.swUpdate.versionUpdates
        .pipe(filter((event): event is VersionReadyEvent => event.type === 'VERSION_READY'))
        .subscribe(() => {
          this.snackBar.open('A new version of Open Planning Poker is ready.', 'Reload', { duration: 15000 })
            .onAction().subscribe(() => document.location.reload());
        });
    }
  }

  /** Opens the browser's install dialog (from the header menu or the weekly offer). */
  async install(): Promise<void> {
    const event = this.installEvent;
    if (!event) return;
    this.installEvent = null;
    this.canInstall.set(false);
    await event.prompt();
    await event.userChoice;
  }

  private offerInstall(): void {
    if (!this.installEvent || !this.installOfferDue()) return;
    this.rememberInstallOffer();
    this.snackBar.open('Install Open Planning Poker on this device to open it like an app.', 'Install', { duration: 12000 })
      .onAction().subscribe(() => void this.install());
  }

  private installOfferDue(): boolean {
    try {
      const last = Number(localStorage.getItem(INSTALL_PROMPT_KEY) ?? 0);
      return Date.now() - last >= INSTALL_PROMPT_INTERVAL_MS;
    } catch {
      return false; // Storage blocked: never nag.
    }
  }

  private rememberInstallOffer(): void {
    try { localStorage.setItem(INSTALL_PROMPT_KEY, String(Date.now())); } catch { /* storage blocked */ }
  }
}
