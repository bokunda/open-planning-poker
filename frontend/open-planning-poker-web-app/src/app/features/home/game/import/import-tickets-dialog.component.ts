import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatTabsModule } from '@angular/material/tabs';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

export interface ImportedTicket {
  name: string;
  description: string;
}

@Component({
  selector: 'app-import-tickets-dialog',
  templateUrl: './import-tickets-dialog.component.html',
  styleUrl: './import-tickets-dialog.component.scss',
  standalone: true,
  imports: [CommonModule, FormsModule, MatDialogModule, MatTabsModule, MatButtonModule, MatIconModule]
})
export class ImportTicketsDialogComponent {
  readonly dialogRef = inject(MatDialogRef<ImportTicketsDialogComponent>);

  parsedTickets: ImportedTicket[] = [];
  bulkText = '';
  importing = false;
  activeTab = 0;

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    const reader = new FileReader();
    reader.onload = () => {
      const content = reader.result as string;
      this.parsedTickets = this.parseContent(content, file.name);
    };
    reader.readAsText(file);
    input.value = '';
  }

  onBulkTextChange(): void {
    if (!this.bulkText.trim()) {
      this.parsedTickets = [];
      return;
    }
    this.parsedTickets = this.parseContent(this.bulkText, '');
  }

  onPaste(event: ClipboardEvent): void {
    // Allow natural paste, then parse
    setTimeout(() => this.onBulkTextChange(), 0);
  }

  private parseContent(content: string, fileName: string): ImportedTicket[] {
    const tickets: ImportedTicket[] = [];

    // Try JSON first
    if (fileName.endsWith('.json') || content.trim().startsWith('[')) {
      try {
        const json = JSON.parse(content);
        if (Array.isArray(json)) {
          return json.map(item => ({
            name: String(item.name || item.title || item.ticket || '').trim(),
            description: String(item.description || item.desc || '').trim()
          })).filter(t => t.name);
        }
      } catch { /* fall through to CSV/line parsing */ }
    }

    // Parse as CSV or line-by-line
    const lines = content.split(/\r?\n/);
    const isCSV = fileName.endsWith('.csv') || (lines.length > 0 && lines[0].includes(','));

    for (const line of lines) {
      const trimmed = line.trim();
      if (!trimmed) continue;

      let name = '';
      let description = '';

      if (isCSV) {
        // CSV: name,description
        const parts = this.splitCSVLine(trimmed);
        name = (parts[0] || '').trim();
        description = (parts[1] || '').trim();
      } else {
        // Bulk: name | description
        const pipeIdx = trimmed.indexOf('|');
        if (pipeIdx >= 0) {
          name = trimmed.substring(0, pipeIdx).trim();
          description = trimmed.substring(pipeIdx + 1).trim();
        } else {
          name = trimmed;
        }
      }

      // Skip header row in CSV
      if (isCSV && name.toLowerCase() === 'name' && description.toLowerCase() === 'description') {
        continue;
      }

      if (name) {
        tickets.push({ name, description });
      }
    }

    return tickets;
  }

  private splitCSVLine(line: string): string[] {
    const result: string[] = [];
    let current = '';
    let inQuotes = false;

    for (const ch of line) {
      if (ch === '"') {
        inQuotes = !inQuotes;
      } else if (ch === ',' && !inQuotes) {
        result.push(current);
        current = '';
      } else {
        current += ch;
      }
    }
    result.push(current);
    return result;
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    const file = event.dataTransfer?.files?.[0];
    if (!file) return;

    const reader = new FileReader();
    reader.onload = () => {
      this.parsedTickets = this.parseContent(reader.result as string, file.name);
    };
    reader.readAsText(file);
    this.activeTab = 1; // switch to File tab
  }

  import(): void {
    if (this.parsedTickets.length === 0) return;
    this.dialogRef.close(this.parsedTickets);
  }
}
