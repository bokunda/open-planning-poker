import { formatDate, registerLocaleData } from '@angular/common';
import localeDe from '@angular/common/locales/de';
import localeSrLatn from '@angular/common/locales/sr-Latn';
import { Pipe, PipeTransform, inject } from '@angular/core';
import { TranslocoService } from '@jsverse/transloco';

registerLocaleData(localeDe, 'de');
registerLocaleData(localeSrLatn, 'sr-Latn');

const LOCALES: Record<string, string> = { en: 'en-US', sr: 'sr-Latn', de: 'de' };

/** Like the `date` pipe, in the active language (day and month names, order). */
@Pipe({ name: 'oppDate', standalone: true, pure: false })
export class OppDatePipe implements PipeTransform {
  private readonly transloco = inject(TranslocoService);

  transform(value: string | number | Date | null | undefined, format = 'mediumDate'): string {
    if (value === null || value === undefined || value === '') return '';
    return formatDate(value, format, LOCALES[this.transloco.getActiveLang()] ?? 'en-US');
  }
}
