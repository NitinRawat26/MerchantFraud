import { DatePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import type { FraudScoreResult } from '../core/models';
import { FraudApiService } from '../core/fraud-api.service';
import { ScoreExplanation } from './score-explanation';

@Component({
  selector: 'mf-alerts',
  imports: [DatePipe, RouterLink, ScoreExplanation],
  template: `
    <section class="page">
      <h1>Alerts queue</h1>
      @if (alerts().length === 0) {
        <p class="muted">No alerts.</p>
      } @else {
        <table>
          <thead><tr><th>Scored at</th><th>Merchant</th><th>Score</th><th>Band</th><th>Model</th><th>Reasons</th></tr></thead>
          <tbody>
            @for (alert of alerts(); track alert.id) {
              <tr>
                <td>{{ alert.scoredAt | date:'medium' }}</td>
                <td><a [routerLink]="['/merchants', alert.subjectId]">{{ alert.subjectId }}</a></td>
                <td>{{ alert.value }}</td>
                <td><span class="badge" [class]="'badge ' + alert.band.toLowerCase()">{{ alert.band }}</span></td>
                <td>{{ alert.modelVersion }}</td>
                <td><mf-score-explanation [reasonCodes]="alert.reasonCodes" /></td>
              </tr>
            }
          </tbody>
        </table>
      }
    </section>
  `
})
export class Alerts {
  private readonly api = inject(FraudApiService);
  protected readonly alerts = toSignal(this.api.alerts(), { initialValue: [] as FraudScoreResult[] });
}
