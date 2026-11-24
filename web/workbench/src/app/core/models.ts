export interface FraudRiskScoreView {
  value: number;
  band: string;
  reasonCodes: string[];
}

export interface FraudScoreResult {
  id: string;
  subjectType: string;
  subjectId: string;
  score: FraudRiskScoreView;
  scoredAt: string;
  modelVersion: string;
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
