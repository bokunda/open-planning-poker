import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialog } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { OPP_APP_VERSION, OPP_GITHUB_REPO_URL, OPP_WEBSITE_URL } from '../../../shared/constants';
import { ThemeService } from '../../../shared/theme.service';

@Component({
  selector: 'app-footer',
  templateUrl: './footer.component.html',
  styleUrl: './footer.component.scss',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule]
})
export class FooterComponent {
  private readonly dialog = inject(MatDialog);
  readonly themeService = inject(ThemeService);

  readonly appVersion = OPP_APP_VERSION;
  readonly githubUrl = OPP_GITHUB_REPO_URL;
  readonly websiteUrl = OPP_WEBSITE_URL;
  readonly currentYear = new Date().getFullYear();

  get isDark(): boolean {
    return this.themeService.isDark;
  }

  toggleTheme(): void {
    this.themeService.toggle();
  }

  async openInfoDialog(): Promise<void> {
    const { InfoDialogComponent } = await import('../../../shared/dialogs/info/info.component');
    this.dialog.open(InfoDialogComponent, {
      maxWidth: '900px',
      enterAnimationDuration: '150ms',
      exitAnimationDuration: '150ms'
    });
  }
}
