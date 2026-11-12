import { Component, inject, OnInit } from '@angular/core';
import { Meta } from '@angular/platform-browser';
import { RouterModule } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';

@Component({
  selector: 'app-errors',
  imports: [TranslocoPipe, RouterModule],
  templateUrl: './errors.component.html',
  styleUrl: './errors.component.scss'
})
export class ErrorsComponent implements OnInit {
  private readonly meta = inject(Meta);

  ngOnInit(): void {
    this.meta.addTag({ name: 'robots', content: 'noindex' });
  }
}
