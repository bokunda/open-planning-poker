import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class ConfigService {

  private config: any;

  constructor() {}

  getConfig() {
    return this.config;
  }
}
