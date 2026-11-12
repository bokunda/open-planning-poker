import { Component, inject, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialog } from '@angular/material/dialog';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatDividerModule } from '@angular/material/divider';
import { RouterLink } from '@angular/router';
import { Apollo } from 'apollo-angular';
import { OPP_GITHUB_REPO_URL, OPP_SUPPORT, OPP_WEBSITE_URL } from '../../../shared/constants';
import { ThemeService } from '../../../shared/theme.service';
import { DonateClickDirective } from '../../../shared/analytics';
import { CHANGE_USERNAME } from '../user/gql/changeUsername.graphql';
import { GET_USER } from '../user/gql/getUser.graphql';
import { ChangeUsernameInput, Mutation } from '../../../graphql/graphql-gateway.service';

import { PwaService } from '../../../shared/pwa/pwa.service';
@Component({
  selector: 'app-header',
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss',
  standalone: true,
  imports: [CommonModule, RouterLink, MatToolbarModule, MatButtonModule, MatIconModule, MatMenuModule, MatDividerModule, DonateClickDirective]
})
export class HeaderComponent {
  @Input() username: string = '';

  readonly dialog = inject(MatDialog);
  readonly themeService = inject(ThemeService);
  readonly pwa = inject(PwaService);

  constructor(private apollo: Apollo) {}

  githubRepoUrl = OPP_GITHUB_REPO_URL;
  websiteUrl = OPP_WEBSITE_URL;
  readonly support = OPP_SUPPORT;

  toggleTheme(): void {
    this.themeService.toggle();
  }

  get isDark(): boolean {
    return this.themeService.isDark;
  }

  async openInfoDialog(): Promise<void> {
    const { InfoDialogComponent } = await import('../../../shared/dialogs/info/info.component');
    this.dialog.open(InfoDialogComponent, {
      maxWidth: '900px',
      enterAnimationDuration: '150ms',
      exitAnimationDuration: '150ms'
    });
  }

  async handleUpdateUser(): Promise<void> {
    const { UpdateUserDialogComponent } = await import('../user/update/dialog/update-user-dialog.component');
    const dialogRef = this.dialog.open(UpdateUserDialogComponent, {
      width: '400px',
      maxWidth: '400px',
      enterAnimationDuration: '150ms',
      exitAnimationDuration: '150ms'
    });

    dialogRef.afterClosed().subscribe((result: ChangeUsernameInput | undefined) => {
      if (!result) return;
      this.apollo.mutate<Mutation, ChangeUsernameInput>({
        mutation: CHANGE_USERNAME,
        variables: { username: result.username },
        refetchQueries: [{ query: GET_USER }]
      }).subscribe({
        next: ({ data }) => {
          if (data?.changeUsername?.boolean) {
            this.username = result.username;
          }
        }
      });
    });
  }
}

