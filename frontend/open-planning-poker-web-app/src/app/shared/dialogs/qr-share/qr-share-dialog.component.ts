import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

export interface QrShareData {
  url: string;
  gameName: string;
}

@Component({
  selector: 'app-qr-share-dialog',
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatButtonModule, MatIconModule],
  template: `
    <div class="qr-share-wrapper">
      <h2 mat-dialog-title>Share Game: {{ data.gameName }}</h2>
      <mat-dialog-content>
        <div class="qr-container">
          <img
            [src]="qrImageUrl"
            alt="QR code to share this game"
            width="220" height="220"
            class="qr-image" />
        </div>
        <div class="qr-url">
          <code>{{ data.url }}</code>
        </div>
        <button mat-stroked-button color="primary" (click)="copyUrl()" class="copy-btn">
          <mat-icon>content_copy</mat-icon> Copy Link
        </button>
        <span class="copied-msg" *ngIf="copied">Copied!</span>
      </mat-dialog-content>
      <mat-dialog-actions align="end">
        <button mat-button mat-dialog-close>Close</button>
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
