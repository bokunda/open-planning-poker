import { Component } from '@angular/core';
import { OPP_GITHUB_REPO_URL, OPP_WEBSITE_URL } from '../../../shared/constants';

@Component({
  selector: 'app-header',
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss',
  standalone: false,
})
export class HeaderComponent {

  githubRepoUrl = OPP_GITHUB_REPO_URL;
  websiteUrl = OPP_WEBSITE_URL;

}
