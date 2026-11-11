import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SeoService } from './shared/seo.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
  standalone: true,
  imports: [RouterOutlet]
})
export class AppComponent {
  title = 'open-planning-poker-ui';

  constructor() {
    inject(SeoService).init();
  }
}
