import { Component, inject, ChangeDetectionStrategy } from '@angular/core';

import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { JiraImportComponent } from './jira-import.component';
import { TrackerImportComponent } from './tracker-import.component';
import { Apollo } from 'apollo-angular';
import { TRACKERS, TrackerInfo } from '../gql/trackers.graphql';
import { MatTabsModule } from '@angular/material/tabs';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { TranslocoPipe } from '@jsverse/transloco';

export interface ImportedTicket {
  name: string;
  description: string;
}

export interface ImportTicketsDialogData {
  /** The game the "From Jira" tab imports into; without it the tab is hidden. */
  gameId: string;
}

/** The dialog closes with tickets to create, or with how many tickets the Jira import already added. */
export type ImportTicketsResult = ImportedTicket[] | { jiraImported: number; trackerName?: string };

@Component({
  selector: 'app-import-tickets-dialog',
  templateUrl: './import-tickets-dialog.component.html',
  styleUrl: './import-tickets-dialog.component.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.Eager,
  imports: [TranslocoPipe, FormsModule, MatDialogModule, MatTabsModule, MatButtonModule, MatIconModule, JiraImportComponent, TrackerImportComponent]
})
export class ImportTicketsDialogComponent {
  readonly dialogRef = inject(MatDialogRef<ImportTicketsDialogComponent>);
  readonly data = inject<ImportTicketsDialogData | null>(MAT_DIALOG_DATA, { optional: true });

  /** Trackers next to Jira that this server has set up. */
  trackers: TrackerInfo[] = [];

  constructor() {
    if (this.data?.gameId) {
      inject(Apollo).query<{ trackers: TrackerInfo[] }>({ query: TRACKERS }).subscribe(({ data }) => {
        this.trackers = (data?.trackers ?? []).filter(t => t.enabled);
      });
    }
  }

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

  onTrackerImported(count: number, trackerName: string): void {
    this.dialogRef.close({ jiraImported: count, trackerName } satisfies ImportTicketsResult);
  }

  onJiraImported(count: number): void {
    this.dialogRef.close({ jiraImported: count } satisfies ImportTicketsResult);
  }
}
