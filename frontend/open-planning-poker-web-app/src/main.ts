import { bootstrapApplication } from '@angular/platform-browser';
import { AppComponent } from './app/app.component';
import { appConfig } from './app/app.config';

fetch('/assets/config.json')
  .then((response) => response.json())
  .then((config) => {
    (window as any).appConfig = config;
    bootstrapApplication(AppComponent, appConfig)
      .catch((err) => console.error(err));
  });
