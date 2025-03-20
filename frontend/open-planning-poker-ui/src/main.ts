import { platformBrowserDynamic } from '@angular/platform-browser-dynamic';
import { AppModule } from './app/app.module';

fetch('/assets/config.json')
  .then((response) => response.json())
  .then((config) => {
    (window as any).appConfig = config;
    platformBrowserDynamic()
      .bootstrapModule(AppModule)
      .catch((err) => console.error(err));
  })
