import { Component, Inject, OnInit } from '@angular/core';
import { MAT_DIALOG_DATA } from '@angular/material/dialog';
import * as QRCode from 'qrcode';

export interface QrShareData {
  url: string;
  gameName: string;
}

@Component({
  selector: 'app-qr-share-dialog',
  template: `
    <div class="qr-share-wrapper">
      <h2 mat-dialog-title>Share Game: {{ data.gameName }}</h2>
      <mat-dialog-content>
        <div class="qr-container">
          <canvas #qrCanvas></canvas>
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
    .qr-url { margin: 8px 0; }
    .qr-url code { word-break: break-all; font-size: 0.8rem; }
    .copy-btn { margin-top: 8px; }
    .copied-msg { color: #4caf50; margin-left: 8px; font-weight: 600; }
  `],
  standalone: false
})
export class QrShareDialogComponent implements OnInit {
  copied = false;

  constructor(@Inject(MAT_DIALOG_DATA) public data: QrShareData) {}

  async ngOnInit(): Promise<void> {
    const canvas = document.querySelector('canvas') as HTMLCanvasElement | null;
    if (canvas) {
      await QRCode.toCanvas(canvas, this.data.url, {
        width: 220,
        margin: 2,
        color: { dark: '#7e3af2', light: '#ffffff' }
      });
    }
  }

  copyUrl(): void {
    navigator.clipboard.writeText(this.data.url).then(() => {
      this.copied = true;
      setTimeout(() => this.copied = false, 2000);
    });
  }
}
