import { Component, inject, OnInit } from '@angular/core';
import { Meta } from '@angular/platform-browser';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-errors',
  imports: [RouterModule],
  templateUrl: './errors.component.html',
  styleUrl: './errors.component.scss'
})
export class ErrorsComponent implements OnInit {
  private readonly meta = inject(Meta);

  title = '404 - Page Not Found';
  message = 'The page you are looking for does not exist or has been moved.';

  ngOnInit(): void {
    this.meta.addTag({ name: 'robots', content: 'noindex' });
  }
}
