import { Component, inject, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialog } from '@angular/material/dialog';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatDividerModule } from '@angular/material/divider';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Apollo } from 'apollo-angular';
import { OPP_GITHUB_REPO_URL, OPP_WEBSITE_URL } from '../../../shared/constants';
import { InfoDialogComponent } from '../../../shared/dialogs/info/info.component';
import { UpdateUserDialogComponent } from '../user/update/dialog/update-user-dialog.component';
import { ThemeService } from '../../../shared/theme.service';
import { CHANGE_USERNAME } from '../user/gql/changeUsername.graphql';
import { GET_USER } from '../user/gql/getUser.graphql';
import { ChangeUsernameInput, Mutation } from '../../../graphql/graphql-gateway.service';

@Component({
  selector: 'app-header',
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss',
  standalone: true,
  imports: [CommonModule, MatToolbarModule, MatButtonModule, MatIconModule, MatMenuModule, MatDividerModule, MatTooltipModule]
})
export class HeaderComponent {
  @Input() username: string = '';

  readonly dialog = inject(MatDialog);
  readonly themeService = inject(ThemeService);

  constructor(private apollo: Apollo) {}

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

  handleUpdateUser(): void {
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

