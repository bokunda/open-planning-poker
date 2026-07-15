import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

@Component({
  selector: 'app-loading',
  templateUrl: './loading.component.html',
  styleUrl: './loading.component.scss',
  standalone: true,
  imports: [CommonModule, MatProgressSpinnerModule]
})
export class LoadingComponent {
  @Input() message: string = '';
  @Input() diameter: number = 40;
  @Input() strokeWidth: number = 3;
}
