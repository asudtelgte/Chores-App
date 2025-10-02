export enum ChoreCategory {
  Daily = 'DAILY',
  Weekly = 'WEEKLY',
  Monthly = 'MONTHLY',
  Special = 'SPECIAL',
}

export interface RecurrencePattern {
  dayInterval?: number;
  daysOfWeek?: string[];
  dayOfMonth?: number;
  relativePattern?: string;
}

// Forward declare to avoid circular reference issues
export interface Chore {
  id: number;
  name: string;
  description?: string;
  location?: string;
  category: ChoreCategory;
  recurrencePattern?: RecurrencePattern;
  deadline?: string;
  createdDate: string;
  isActive: boolean;
  isCompletedForPeriod: boolean;
  completions: ChoreCompletion[];
}

export interface ChoreCompletion {
  id: number;
  choreId: number;
  completionDate: string;
  necessityRating: number;
  notes?: string;
  chore?: Chore;
}

export interface ChoreInput {
  name: string;
  description?: string;
  location?: string;
  category: ChoreCategory;
  recurrencePattern?: RecurrencePattern;
  deadline?: string;
}

export interface CompleteChoreInput {
  choreId: number;
  necessityRating: number;
  notes?: string;
}
