import { Component } from '@angular/core';
import { PageHeading } from '../core/page-heading';

interface KnownLimitation {
  problem: string;
  criteria: string;
  plannedFix: string;
}

// Keep this page in step with the audit: update it whenever a limitation is fixed or found.
@Component({
  selector: 'app-accessibility-page',
  imports: [PageHeading],
  templateUrl: './accessibility-page.html',
})
export class AccessibilityPage {
  protected readonly reviewedOn = '15 November 2026';
  protected readonly contactEmail = 'accessibility@example.com';

  protected readonly limitations: KnownLimitation[] = [
    {
      problem: 'Messages such as "Added" and the number of search results are not announced by screen readers.',
      criteria: '4.1.3',
      plannedFix: 'January 2027',
    },
    {
      problem: 'Errors returned by the server, such as missing stock, are shown below the form but are not announced.',
      criteria: '3.3.1, 4.1.3',
      plannedFix: 'January 2027',
    },
  ];
}
