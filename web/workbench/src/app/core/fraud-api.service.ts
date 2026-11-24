import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import type { FraudCaseRecord, FraudScoreResult, HealthStatus } from './models';

@Injectable({ providedIn: 'root' })
export class FraudApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/';

  health(): Observable<HealthStatus> {
    return this.http.get<HealthStatus>(`${this.baseUrl}health`);
  }

  alerts(): Observable<FraudScoreResult[]> {
    return this.http.get<FraudScoreResult[]>(`${this.baseUrl}alerts`);
  }

  cases(): Observable<FraudCaseRecord[]> {
    return this.http.get<FraudCaseRecord[]>(`${this.baseUrl}cases`);
  }
}
