import { DatePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { combineLatest, map } from 'rxjs';
import type { FraudCaseRecord, FraudScoreResult } from '../core/models';
import { FraudApiService } from '../core/fraud-api.service';
import { ScoreExplanation } from './score-explanation';

interface MerchantDetailData {
  merchantId: string;
  alerts: FraudScoreResult[];
  cases: FraudCaseRecord[];
}

@Component({
  selector: 'mf-merchant-detail',
  imports: [DatePipe, ScoreExplanation],
  template: `
    <section class="page">
      <h1>Merchant {{ data().merchantId || 'detail' }}</h1>
      <div class="cards">
        <div class="card"><span>Scores</span><strong>{{ data().alerts.length }}</strong></div>
        <div class="card"><span>Cases</span><strong>{{ data().cases.length }}</strong></div>
        <div class="card"><span>Latest score</span><strong>{{ latestScore() }}</strong></div>
      </div>

      <h2>Score explanations</h2>
      @if (data().alerts.length === 0) {
        <p class="muted">No score results for this merchant.</p>
      } @else {
        <table>
          <thead><tr><th>Scored at</th><th>Score</th><th>Band</th><th>Model</th><th>Reasons</th></tr></thead>
          <tbody>
            @for (alert of data().alerts; track alert.id) {
              <tr>
                <td>{{ alert.scoredAt | date:'medium' }}</td>
                <td>{{ alert.score.value }}</td>
                <td><span class="badge" [class]="'badge ' + alert.score.band.toLowerCase()">{{ alert.score.band }}</span></td>
                <td>{{ alert.modelVersion }}</td>
                <td><mf-score-explanation [reasonCodes]="alert.score.reasonCodes" /></td>
              </tr>
            }
          </tbody>
        </table>
      }

      <h2>Cases</h2>
      @if (data().cases.length === 0) {
        <p class="muted">No cases for this merchant.</p>
      } @else {
        <table>
          <thead><tr><th>Opened</th><th>Title</th><th>Priority</th><th>Status</th><th>Assignee</th></tr></thead>
          <tbody>
            @for (fraudCase of data().cases; track fraudCase.id) {
              <tr>
                <td>{{ fraudCase.openedAt | date:'medium' }}</td>
                <td>{{ fraudCase.title }}</td>
                <td>{{ fraudCase.priority }}</td>
                <td>{{ fraudCase.status }}</td>
                <td>{{ fraudCase.assignedTo ?? '—' }}</td>
              </tr>
            }
          </tbody>
        </table>
      }
    </section>
  `
})
export class MerchantDetail {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(FraudApiService);

  protected readonly data = toSignal(
    combineLatest([this.route.paramMap, this.api.alerts(), this.api.cases()]).pipe(
      map(([params, alerts, cases]) => {
        const merchantId = params.get('id') ?? '';
        const detail: MerchantDetailData = {
          merchantId,
          alerts: alerts.filter(alert => alert.subjectId === merchantId),
          cases: cases.filter(fraudCase => fraudCase.merchantId === merchantId)
        };
        return detail;
      })
    ),
    { initialValue: { merchantId: '', alerts: [] as FraudScoreResult[], cases: [] as FraudCaseRecord[] } }
  );

  protected latestScore(): number {
    const alerts = this.data().alerts;
    return alerts.length === 0 ? 0 : alerts[0].score.value;
  }
}
