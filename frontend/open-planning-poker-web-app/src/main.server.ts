import { provideZoneChangeDetection } from "@angular/core";
import { BootstrapContext, bootstrapApplication } from '@angular/platform-browser';
import { AppComponent } from './app/app.component';
import { config } from './app/app.config.server';

// Each server render gets its own platform from the context (required since Angular 19.2.16).
const bootstrap = (context: BootstrapContext) => bootstrapApplication(AppComponent, {...config, providers: [provideZoneChangeDetection(), ...config.providers]}, context);

export default bootstrap;
