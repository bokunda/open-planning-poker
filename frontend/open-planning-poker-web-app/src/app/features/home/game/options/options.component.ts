import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-options',
  templateUrl: './options.component.html',
  styleUrl: './options.component.scss',
  standalone: true,
  imports: [CommonModule, MatButtonModule],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OptionsComponent {

  @Input() inGame: boolean = false;

  @Output() onCreateGameClick = new EventEmitter<void>();
  @Output() onJoinGameClick = new EventEmitter<void>();
  @Output() onLeaveGameClick = new EventEmitter<void>();
}
