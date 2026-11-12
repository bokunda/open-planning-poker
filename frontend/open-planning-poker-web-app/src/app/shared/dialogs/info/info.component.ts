import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { OPP_SUPPORT } from '../../constants';
import { DonateClickDirective } from '../../analytics';

@Component({
  selector: 'app-info-dialog',
  imports: [ MatButtonModule, MatDialogModule, MatIconModule, DonateClickDirective ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './info.component.html',
  styleUrl: './info.component.scss'
})
export class InfoDialogComponent {
  readonly support = OPP_SUPPORT;

}
