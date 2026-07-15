import { Component, inject, Input, OnChanges } from '@angular/core';
import { CommonModule, DOCUMENT } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Meta } from '@angular/platform-browser';

export interface BreadcrumbItem {
  label: string;
  url?: string;
}

@Component({
  selector: 'app-breadcrumb',
  templateUrl: './breadcrumb.component.html',
  styleUrl: './breadcrumb.component.scss',
  standalone: true,
  imports: [CommonModule, RouterModule]
})
export class BreadcrumbComponent implements OnChanges {
  @Input() items: BreadcrumbItem[] = [];

  trackByIndex(index: number): number {
    return index;
  }

  private readonly meta = inject(Meta);
  private readonly document = inject(DOCUMENT);

  ngOnChanges(): void {
    this.updateStructuredData();
  }

  private updateStructuredData(): void {
    const scriptId = 'breadcrumb-ld-json';
    const existingScript = this.document.getElementById(scriptId);
    if (existingScript) {
      existingScript.remove();
    }

    const itemList = this.items.map((item, index) => ({
      '@type': 'ListItem',
      'position': index + 1,
      'name': item.label,
      ...(item.url ? { 'item': `https://app.openplanningpoker.com${item.url}` } : {})
    }));

    const script = this.document.createElement('script');
    script.id = scriptId;
    script.type = 'application/ld+json';
    script.text = JSON.stringify({
      '@context': 'https://schema.org',
      '@type': 'BreadcrumbList',
      'itemListElement': itemList
    });
    this.document.head.appendChild(script);
  }
}
