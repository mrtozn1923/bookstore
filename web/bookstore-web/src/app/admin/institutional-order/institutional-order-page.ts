import { HttpClient, httpResource } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { email, Field, form, FormField, min, minDate, minLength, required, submit, validate } from '@angular/forms/signals';
import { MatAutocomplete, MatAutocompleteTrigger } from '@angular/material/autocomplete';
import { MatButton } from '@angular/material/button';
import { ErrorStateMatcher, MAT_DATE_LOCALE, MatOption } from '@angular/material/core';
import { provideDateFnsAdapter } from '@angular/material-date-fns-adapter';
import { tr } from 'date-fns/locale';
import { MatDatepicker, MatDatepickerInput, MatDatepickerToggle } from '@angular/material/datepicker';
import { MAT_FORM_FIELD_DEFAULT_OPTIONS, MatError, MatFormField, MatHint, MatLabel, MatPrefix, MatSuffix } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatInput } from '@angular/material/input';
import { MatSelect } from '@angular/material/select';
import { catchError, debounceTime, distinctUntilChanged, map, of, switchMap } from 'rxjs';
import { BookSummary, InstitutionUnit, PagedResult } from '../../core/api-models';
import { useOverlayThemeClass } from '../../core/overlay-theme';

interface InstitutionalOrderModel {
  units: InstitutionUnit[];
  book: BookSummary | string;
  quantity: number;
  deliveryDate: Date | null;
  contactEmail: string;
  notes: string;
}

const lastUnitsKey = 'bookstore.admin.lastUnits';

const datepickerMessages: Record<string, string> = {
  matDatepickerParse: 'Enter the date as DD.MM.YYYY.',
  matDatepickerMin: 'The earliest delivery date is tomorrow.',
};

@Component({
  selector: 'app-institutional-order-page',
  // Institutional portal theme (material-theme.scss)
  host: { class: 'institutional-portal' },
  imports: [
    FormField, MatFormField, MatLabel, MatHint, MatError, MatPrefix, MatSuffix, MatInput, MatSelect, MatOption,
    MatAutocomplete, MatAutocompleteTrigger, MatDatepicker, MatDatepickerInput, MatDatepickerToggle, MatIcon, MatButton,
  ],
  templateUrl: './institutional-order-page.html',
  styleUrl: './institutional-order-page.scss',
  // Turkish dates: dd.MM.yyyy parsing and display, weeks start on Monday (tr.options.weekStartsOn = 1)
  providers: [
    provideDateFnsAdapter(),
    { provide: MAT_DATE_LOCALE, useValue: tr },
    // One place for the look of every field on the page instead of appearance="outline" on each
    { provide: MAT_FORM_FIELD_DEFAULT_OPTIONS, useValue: { appearance: 'outline' } },
  ],
})
export class InstitutionalOrderPage {
  constructor() {
    useOverlayThemeClass('institutional-portal');
  }

  private readonly http = inject(HttpClient);
  protected readonly tomorrow = startOfTomorrow();

  protected readonly units = httpResource<InstitutionUnit[]>(() => '/api/institutions/1/units', { defaultValue: [] });

  protected readonly model = signal<InstitutionalOrderModel>({
    units: readLastUnits(),
    book: '',
    quantity: 20,
    deliveryDate: null,
    contactEmail: '',
    notes: '',
  });

  protected readonly orderForm = form(this.model, path => {
    // required() treats an empty array as a value; ask for at least one unit explicitly
    minLength(path.units, 1, { message: 'Choose at least one unit.' });
    validate(path.book, ({ value }) =>
      typeof value() === 'string' ? { kind: 'bookNotChosen', message: 'Pick a book from the list.' } : undefined);
    min(path.quantity, 20, { message: 'Institutional orders start at 20 copies.' });
    required(path.deliveryDate, { message: 'Choose a delivery date.' });
    minDate(path.deliveryDate, this.tomorrow, { message: 'The earliest delivery date is tomorrow.' });
    required(path.contactEmail, { message: 'Enter a contact e-mail.' });
    email(path.contactEmail, { message: 'Enter an e-mail address like buyer@example.com.' });
  });

  // Server-side search: 300 ms after the last key stroke, at least 2 characters, the old request is cancelled
  private readonly query = computed(() => {
    const value = this.model().book;
    return typeof value === 'string' ? value.trim() : '';
  });
  protected readonly matches = toSignal(
    toObservable(this.query).pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap(search => search.length < 2
        ? of([])
        : this.http.get<PagedResult<BookSummary>>('/api/books', { params: { search, pageSize: 8 } }).pipe(
            map(result => result.items),
            catchError(() => of([])))),
    ),
    { initialValue: [] },
  );

  protected readonly request = signal('');
  // The e-mail field shows its error while typing; the others after blur or submit (the default matcher)
  protected readonly whileTyping = new WhileTypingErrorStateMatcher();

  // Options come from the API as new objects on every load: compare units by id, not by reference
  protected readonly sameUnit = (a: InstitutionUnit | null, b: InstitutionUnit | null) => a?.id === b?.id;
  protected readonly bookTitle = (book: BookSummary | string | null) => (typeof book === 'string' ? book : book?.title ?? '');

  protected firstError(field: { (): { errors(): { kind: string; message?: string }[] } }): string {
    const error = field().errors()[0];
    // The datepicker's own validators (parse, min, max, filter) arrive without a message
    return error?.message ?? datepickerMessages[error?.kind ?? ''] ?? '';
  }

  protected async save(event: Event): Promise<void> {
    event.preventDefault();
    await submit(this.orderForm, async () => {
      const order = this.model();
      localStorage.setItem(lastUnitsKey, JSON.stringify(order.units));
      this.request.set(JSON.stringify(toRequest(order), null, 2));
      return undefined;
    });
  }
}

// ShowOnDirtyErrorStateMatcher alone would hide "required" on an empty field even after submit:
// submit() marks fields as touched, not dirty
class WhileTypingErrorStateMatcher extends ErrorStateMatcher {
  override isSignalErrorState(field: Field<unknown> | null): boolean {
    const state = field?.();
    return !!state && state.invalid() && (state.dirty() || state.touched());
  }
}

function toRequest(order: InstitutionalOrderModel) {
  const book = order.book as BookSummary;
  return {
    unitIds: order.units.map(unit => unit.id),
    isbn: book.isbn,
    quantity: order.quantity,
    // The picked day as a calendar date; toISOString() would convert local midnight to UTC
    deliveryDate: order.deliveryDate ? toDateOnly(order.deliveryDate) : null,
    contactEmail: order.contactEmail.trim(),
    notes: order.notes.trim(),
  };
}

function toDateOnly(date: Date): string {
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${date.getFullYear()}-${month}-${day}`;
}

function startOfTomorrow(): Date {
  const now = new Date();
  return new Date(now.getFullYear(), now.getMonth(), now.getDate() + 1);
}

function readLastUnits(): InstitutionUnit[] {
  try {
    return JSON.parse(localStorage.getItem(lastUnitsKey) ?? '[]') as InstitutionUnit[];
  } catch {
    return [];
  }
}
