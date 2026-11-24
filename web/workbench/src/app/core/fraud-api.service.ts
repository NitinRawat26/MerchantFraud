import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import type {
  FraudCaseRecord,
  FraudPreset,
  FraudPresetRun,
  FraudScoreResult,
  HealthStatus
} from './models';

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

  presets(): Observable<FraudPreset[]> {
    return this.http.get<FraudPreset[]>(`${this.baseUrl}presets`);
  }

  runPreset(id: string, persist: boolean): Observable<FraudPresetRun> {
    const params = new HttpParams().set('persist', persist);
    return this.http.post<FraudPresetRun>(
      `${this.baseUrl}presets/${encodeURIComponent(id)}/run`,
      null,
      { params }
    );
  }
}
