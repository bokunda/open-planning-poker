import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SeoService } from './shared/seo.service';
import { OfflineNoticeComponent } from './shared/pwa/offline-notice.component';
import { PwaService } from './shared/pwa/pwa.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.Eager,
  imports: [RouterOutlet, OfflineNoticeComponent]
})
export class AppComponent {
  title = 'open-planning-poker-ui';

  constructor() {
    inject(SeoService).init();
    inject(PwaService).init();
  }
}
