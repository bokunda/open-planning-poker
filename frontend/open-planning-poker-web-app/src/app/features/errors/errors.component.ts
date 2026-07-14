import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-errors',
  imports: [RouterModule],
  templateUrl: './errors.component.html',
  styleUrl: './errors.component.scss'
})
export class ErrorsComponent {
  title = '404 - Page Not Found';
  message = 'The page you are looking for does not exist or has been moved.';
}
