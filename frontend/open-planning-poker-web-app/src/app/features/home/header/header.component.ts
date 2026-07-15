import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { OPP_GITHUB_REPO_URL, OPP_WEBSITE_URL } from '../../../shared/constants';
import { InfoDialogComponent } from '../../../shared/dialogs/info/info.component';
import { ThemeService } from '../../../shared/theme.service';

@Component({
  selector: 'app-header',
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss',
  standalone: false,
})
export class HeaderComponent {

  readonly dialog = inject(MatDialog);
  readonly themeService = inject(ThemeService);

  githubRepoUrl = OPP_GITHUB_REPO_URL;
  websiteUrl = OPP_WEBSITE_URL;

  toggleTheme(): void {
    this.themeService.toggle();
  }

  get isDark(): boolean {
    return this.themeService.isDark;
  }

  openInfoDialog() {
    this.dialog.open(InfoDialogComponent, {
      maxWidth: '900px',
      enterAnimationDuration: '150ms',
      exitAnimationDuration: '150ms'
    });
  }
}

