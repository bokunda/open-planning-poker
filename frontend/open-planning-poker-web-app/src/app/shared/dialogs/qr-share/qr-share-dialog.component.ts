import { Component, Inject } from '@angular/core';

import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { TranslocoPipe } from '@jsverse/transloco';

export interface QrShareData {
  url: string;
  gameName: string;
}

@Component({
  selector: 'app-qr-share-dialog',
  standalone: true,
  imports: [TranslocoPipe, MatDialogModule, MatButtonModule, MatIconModule],
  template: `
    <div class="qr-share-wrapper">
      <h2 mat-dialog-title>{{ 'share.title' | transloco }}</h2>
      <mat-dialog-content>
        <p class="opp-dialog-lead">{{ 'share.lead' | transloco: { name: data.gameName } }}</p>
        <div class="qr-container">
          <img
            [src]="qrImageUrl"
            [alt]="'share.qrAlt' | transloco"
            width="220" height="220"
            class="qr-image" />
          </div>
          <div class="qr-url">
            <code>{{ data.url }}</code>
          </div>
          <button mat-flat-button color="primary" (click)="copyUrl()" class="opp-submit copy-btn">
            <mat-icon>content_copy</mat-icon> {{ 'room.copyLinkShort' | transloco }}
          </button>
          @if (copied) {
            <span class="copied-msg">{{ 'share.copied' | transloco }}</span>
          }
        </mat-dialog-content>
        <mat-dialog-actions align="end">
          <button mat-button mat-dialog-close>{{ 'common.close' | transloco }}</button>
        </mat-dialog-actions>
      </div>
    `,
  styles: [`
    .qr-share-wrapper { text-align: center; }
    .qr-container { display: flex; justify-content: center; padding: 16px 0; }
    .qr-image { border-radius: 8px; }
    .qr-url { margin: 8px 0; }
    .qr-url code { word-break: break-all; font-size: 0.8rem; }
    .copy-btn { margin-top: 8px; }
    .copied-msg { color: #4caf50; margin-left: 8px; font-weight: 600; }
  `],
})
export class QrShareDialogComponent {
  copied = false;

  get qrImageUrl(): string {
    const encoded = encodeURIComponent(this.data.url);
    return `https://api.qrserver.com/v1/create-qr-code/?size=220x220&data=${encoded}&color=7e3af2&bgcolor=ffffff&margin=4`;
  }

  constructor(@Inject(MAT_DIALOG_DATA) public data: QrShareData) {}

  copyUrl(): void {
    navigator.clipboard.writeText(this.data.url).then(() => {
      this.copied = true;
      setTimeout(() => this.copied = false, 2000);
    });
  }
}
