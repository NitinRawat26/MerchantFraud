import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import type { FraudCaseRecord, FraudScoreResult } from '../core/models';
import { FraudApiService } from '../core/fraud-api.service';
import { ScoreExplanation } from './score-explanation';

@Component({
  selector: 'mf-dashboard',
  imports: [RouterLink, ScoreExplanation],
  template: `
    <section class="page">
      <h1>Fraud dashboard</h1>
      <div class="cards">
        <div class="card"><span>API</span><strong>{{ health()?.status ?? 'unknown' }}</strong></div>
        <div class="card"><span>High-risk alerts</span><strong>{{ highRiskAlerts().length }}</strong></div>
        <div class="card"><span>Open cases</span><strong>{{ openCases() }}</strong></div>
      </div>

      <h2>Recent alerts</h2>
      @if (recentAlerts().length === 0) {
        <p class="muted">No alerts yet.</p>
      } @else {
        <table>
          <thead><tr><th>Merchant</th><th>Score</th><th>Band</th><th>Model</th><th>Reasons</th></tr></thead>
          <tbody>
            @for (alert of recentAlerts(); track alert.id) {
              <tr>
                <td><a [routerLink]="['/merchants', alert.subjectId]">{{ alert.subjectId }}</a></td>
                <td>{{ alert.score.value }}</td>
                <td><span class="badge" [class]="'badge ' + alert.score.band.toLowerCase()">{{ alert.score.band }}</span></td>
                <td>{{ alert.modelVersion }}</td>
                <td><mf-score-explanation [reasonCodes]="alert.score.reasonCodes" /></td>
              </tr>
            }
          </tbody>
        </table>
      }
    </section>
  `
})
export class Dashboard {
  private readonly api = inject(FraudApiService);
  private readonly alerts = toSignal(this.api.alerts(), { initialValue: [] as FraudScoreResult[] });
  private readonly cases = toSignal(this.api.cases(), { initialValue: [] as FraudCaseRecord[] });
  protected readonly health = toSignal(this.api.health(), { initialValue: null });
  protected readonly recentAlerts = computed(() => this.alerts().slice(0, 5));
  protected readonly highRiskAlerts = computed(() => this.alerts().filter(alert => alert.score.value >= 700));
  protected readonly openCases = computed(() => this.cases().filter(fraudCase => fraudCase.status === 'Open').length);
}
