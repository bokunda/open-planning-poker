import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class ConfigService {

  private config: any;

  constructor(private http: HttpClient) { }

  async loadConfig() {
    const data = await firstValueFrom(this.http.get(url));
    this.config = data;
  }

  getConfig() {
    return this.config;
  }
}
