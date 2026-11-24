export interface FraudScoreResult {
  id: string;
  subjectType: string;
  subjectId: string;
  value: number;
  band: string;
  reasonCodes: string[];
  scoredAt: string;
  modelVersion?: string | null;
}

export interface FraudCaseRecord {
  id: string;
  merchantId: string;
  title: string;
  priority: string;
  status: string;
  assignedTo?: string | null;
  openedAt: string;
  closedAt?: string | null;
  externalRef?: string | null;
}

export interface HealthStatus {
  status: string;
}

export interface FraudPreset {
  id: string;
  name: string;
  description: string;
  focusMerchantId: string;
  merchantIds: string[];
}

export interface PresetMetric {
  label: string;
  value: string;
}

export interface PresetTable {
  title: string;
  columns: string[];
  rows: string[][];
}

export interface PresetStage {
  order: number;
  key: string;
  title: string;
  component: string;
  status: 'ok' | 'warning' | 'skipped' | string;
  summary: string;
  metrics: PresetMetric[];
  tables: PresetTable[];
}

export interface PresetMerchantOutcome {
  merchantId: string;
  name: string;
  scenario: string;
  score: number;
  band: string;
  recommendedAction: string;
  ruleHits: number;
  networkLinkedMerchants: number;
  mlFraudProbability: number;
  merchantIntelligenceDecision: string;
  reasonCodes: string[];
  caseId?: string | null;
}

export interface FraudPresetRun {
  presetId: string;
  presetName: string;
  description: string;
  focusMerchantId: string;
  asOf: string;
  persisted: boolean;
  merchants: PresetMerchantOutcome[];
  stages: PresetStage[];
}
