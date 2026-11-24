import { DatePipe, PercentPipe } from '@angular/common';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import type { FraudPreset, FraudPresetRun, PresetStage } from '../core/models';
import { FraudApiService } from '../core/fraud-api.service';
import { ScoreExplanation } from './score-explanation';

const STAGE_REVEAL_MS = 450;

@Component({
  selector: 'mf-preset-lab',
  imports: [DatePipe, PercentPipe, RouterLink, ScoreExplanation],
  template: `
    <section class="page preset-lab">
      <header class="lab-header">
        <div>
          <h1>Preset Lab</h1>
          <p class="muted">
            Run a scripted fraud scenario end to end and watch each FraudShield component —
            mapping, ingestion, features, rules, network, onboarding signals, ML, scoring and case
            management — produce its own output.
          </p>
        </div>
        <label class="persist-toggle">
          <input type="checkbox" [checked]="persist()" (change)="persist.set(!persist())" />
          Save results to Alerts &amp; Cases
        </label>
      </header>

      <div class="preset-grid">
        @for (preset of presets(); track preset.id) {
          <button
            type="button"
            class="preset-card"
            [class.selected]="preset.id === selectedId()"
            [disabled]="running()"
            (click)="run(preset)"
          >
            <strong>{{ preset.name }}</strong>
            <span>{{ preset.description }}</span>
            <em>{{ preset.merchantIds.length }} merchant(s) · Run ▶</em>
          </button>
        }
      </div>

      @if (error()) {
        <p class="lab-error">{{ error() }}</p>
      }

      @if (result(); as run) {
        <section class="lab-section">
          <h2>{{ run.presetName }}</h2>
          <p class="muted">
            Evaluated as of {{ run.asOf | date: 'medium' : 'UTC' }} UTC ·
            {{ run.persisted ? 'saved to SQLite' : 'dry run (nothing saved)' }}
            @if (run.persisted) {
              · <a routerLink="/alerts">open Alerts queue</a>
            }
          </p>

          <ol class="pipeline" aria-label="Pipeline progress">
            @for (stage of run.stages; track stage.key) {
              <li
                [class]="'pipeline-step ' + (stage.order <= revealed() ? stage.status : 'pending')"
                [title]="stage.component"
              >
                <span class="dot">{{ stage.order <= revealed() ? statusIcon(stage) : stage.order }}</span>
                <span class="label">{{ stage.title }}</span>
              </li>
            }
          </ol>
        </section>

        <section class="lab-section">
          @for (stage of run.stages; track stage.key) {
            @if (stage.order <= revealed()) {
              <article class="stage-card" [class]="'stage-card ' + stage.status">
                <header (click)="toggle(stage.key)">
                  <span class="stage-order">{{ stage.order }}</span>
                  <div class="stage-title">
                    <strong>{{ stage.title }}</strong>
                    <code>{{ stage.component }}</code>
                  </div>
                  <span [class]="'status-pill ' + stage.status">{{ stage.status }}</span>
                  <span class="chevron">{{ isOpen(stage.key) ? '▾' : '▸' }}</span>
                </header>
                <p class="stage-summary">{{ stage.summary }}</p>
                <div class="stage-metrics">
                  @for (metric of stage.metrics; track metric.label) {
                    <div class="metric">
                      <span>{{ metric.label }}</span>
                      <strong>{{ metric.value }}</strong>
                    </div>
                  }
                </div>
                @if (isOpen(stage.key)) {
                  @for (table of stage.tables; track table.title) {
                    <h4>{{ table.title }}</h4>
                    @if (table.rows.length === 0) {
                      <p class="muted">None.</p>
                    } @else {
                      <div class="table-scroll">
                        <table class="compact">
                          <thead>
                            <tr>
                              @for (column of table.columns; track $index) {
                                <th>{{ column }}</th>
                              }
                            </tr>
                          </thead>
                          <tbody>
                            @for (row of table.rows; track $index) {
                              <tr>
                                @for (cell of row; track $index) {
                                  <td>{{ cell }}</td>
                                }
                              </tr>
                            }
                          </tbody>
                        </table>
                      </div>
                    }
                  }
                }
              </article>
            }
          }
        </section>

        @if (revealed() >= run.stages.length) {
          <section class="lab-section">
            <h2>Outcome by merchant</h2>
            <table>
              <thead>
                <tr>
                  <th>Merchant</th>
                  <th>Scenario</th>
                  <th>Score</th>
                  <th>Band</th>
                  <th>Rule action</th>
                  <th>Network links</th>
                  <th>ML probability</th>
                  <th>Onboarding</th>
                  <th>Case</th>
                  <th>Reasons</th>
                </tr>
              </thead>
              <tbody>
                @for (merchant of run.merchants; track merchant.merchantId) {
                  <tr [class.focus-row]="merchant.merchantId === run.focusMerchantId">
                    <td>
                      @if (run.persisted) {
                        <a [routerLink]="['/merchants', merchant.merchantId]">{{ merchant.name }}</a>
                      } @else {
                        {{ merchant.name }}
                      }
                      <div class="muted small">{{ merchant.merchantId }}</div>
                    </td>
                    <td>{{ merchant.scenario }}</td>
                    <td><strong>{{ merchant.score }}</strong></td>
                    <td>
                      <span [class]="'badge ' + merchant.band.toLowerCase()">{{ merchant.band }}</span>
                    </td>
                    <td>{{ merchant.recommendedAction }} ({{ merchant.ruleHits }} hits)</td>
                    <td>{{ merchant.networkLinkedMerchants }}</td>
                    <td>{{ merchant.mlFraudProbability | percent: '1.0-0' }}</td>
                    <td>{{ merchant.merchantIntelligenceDecision }}</td>
                    <td>{{ merchant.caseId ?? '—' }}</td>
                    <td><mf-score-explanation [reasonCodes]="merchant.reasonCodes" /></td>
                  </tr>
                }
              </tbody>
            </table>
          </section>
        }
      } @else if (!running()) {
        <p class="muted">Pick a preset above to run it.</p>
      }
    </section>
  `
})
export class PresetLab {
  private readonly api = inject(FraudApiService);
  private readonly destroyRef = inject(DestroyRef);
  private revealTimer?: ReturnType<typeof setInterval>;

  protected readonly presets = toSignal(this.api.presets(), {
    initialValue: [] as FraudPreset[]
  });
  protected readonly persist = signal(true);
  protected readonly selectedId = signal<string | null>(null);
  protected readonly running = signal(false);
  protected readonly result = signal<FraudPresetRun | null>(null);
  protected readonly revealed = signal(0);
  protected readonly error = signal<string | null>(null);
  private readonly open = signal<ReadonlySet<string>>(new Set());

  constructor() {
    this.destroyRef.onDestroy(() => this.stopReveal());
  }

  protected run(preset: FraudPreset): void {
    this.stopReveal();
    this.selectedId.set(preset.id);
    this.running.set(true);
    this.error.set(null);
    this.result.set(null);
    this.revealed.set(0);
    this.open.set(new Set());

    this.api.runPreset(preset.id, this.persist()).subscribe({
      next: (run) => {
        this.result.set(run);
        this.revealStages(run.stages.length);
      },
      error: () => {
        this.running.set(false);
        this.error.set(`Preset "${preset.name}" failed to run. Is the API running?`);
      }
    });
  }

  protected toggle(key: string): void {
    const next = new Set(this.open());
    if (next.has(key)) {
      next.delete(key);
    } else {
      next.add(key);
    }
    this.open.set(next);
  }

  protected isOpen(key: string): boolean {
    return this.open().has(key);
  }

  protected statusIcon(stage: PresetStage): string {
    switch (stage.status) {
      case 'ok':
        return '✓';
      case 'warning':
        return '!';
      default:
        return '–';
    }
  }

  private revealStages(total: number): void {
    this.revealed.set(1);
    this.revealTimer = setInterval(() => {
      const next = this.revealed() + 1;
      this.revealed.set(next);
      if (next >= total) {
        this.stopReveal();
      }
    }, STAGE_REVEAL_MS);
    if (total <= 1) {
      this.stopReveal();
    }
  }

  private stopReveal(): void {
    if (this.revealTimer !== undefined) {
      clearInterval(this.revealTimer);
      this.revealTimer = undefined;
    }
    this.running.set(false);
  }
}
