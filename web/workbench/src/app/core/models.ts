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
