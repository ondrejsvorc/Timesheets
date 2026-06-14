export interface TimeRange {
  start: string;
  end: string;
}

export interface Attendance {
  clockIn: string;
  clockOut: string;
  breakStart: string;
  breakEnd: string;
  interruptions: string;
  nightHours: number;
  schedules: TimeRange[];
}

export interface CoreDefinition {
  workload: number;
}

export interface ProjectDefinition {
  id: string;
  registrationNumber: string;
  name: string;
  position: string;
  workload: number;
  lockedAt: string | null;
  lockedBy: string | null;
}

export interface TimesheetDay {
  date: string;
  attendance: Attendance;
  coreHours: number | null;
  projectHours: Record<string, number>;
  isHoliday: boolean;
  isWeekend: boolean;
}

export interface Timesheet {
  id: string;
  year: number;
  month: number;
  totalWorkload: number;
  hasBaseWorkload: boolean;
  core: CoreDefinition;
  projects: ProjectDefinition[];
  days: TimesheetDay[];
}
