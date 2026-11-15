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

  // Status messages, the error summary and server errors were fixed in November 2026 (WCAG 3.3.1, 4.1.3)
  protected readonly limitations: KnownLimitation[] = [];
}
