import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { OPP_GITHUB_REPO_URL, OPP_WEBSITE_URL } from '../../../shared/constants';
import { MatButtonModule } from '@angular/material/button';
import { InfoDialogComponent } from '../../../shared/dialogs/info/info.component';

@Component({
  selector: 'app-header',
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss',
  standalone: false,
})
export class HeaderComponent {

  readonly dialog = inject(MatDialog);

  githubRepoUrl = OPP_GITHUB_REPO_URL;
  websiteUrl = OPP_WEBSITE_URL;

  openInfoDialog() {
    const dialogRef = this.dialog.open(InfoDialogComponent, {
      maxWidth: '900px',
      enterAnimationDuration: '150ms',
      exitAnimationDuration: '150ms'
    });
  }
}

