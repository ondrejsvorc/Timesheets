export type CombinedTimesheet = {
  year: number;
  month: number; // 1–12
  days: CombinedDay[];

  totalHours: number;
  totalWorkload: number;
  totalHoursObligation: number;
};

export type CombinedDay = {
  date: Date;
  isHoliday: boolean;
  isWeekend: boolean;
  isWorkday: boolean;

  attendanceHours: number;
  projectHours: number;

  attendanceWorkload: number;
  projectWorkload: number;
};

const DAY_NAMES_CZ = ["Ne", "Po", "Út", "St", "Čt", "Pá", "So"] as const;
export const getDayName = (date: Date) => DAY_NAMES_CZ[date.getDay()];

const STANDARD_WORKDAY_HOURS = 8;
const isWeekend = (date: Date) => date.getDay() === 0 || date.getDay() === 6;
const isWorkday = (date: Date, isHoliday: boolean) => !isWeekend(date) && !isHoliday;
const hoursObligation = (workload: number, date: Date, isHoliday: boolean) => (isWorkday(date, isHoliday) ? STANDARD_WORKDAY_HOURS * workload : 0);

type ProjectInput = {
  id: string;
  workload: number; // např. 0.25, 0.5
};

export const generateCombinedTimesheet = (year: number, month: number, workload: number, projects: ProjectInput[]): CombinedTimesheet => {
  const daysInMonth = new Date(year, month, 0).getDate();
  const days: CombinedDay[] = [];

  for (let d = 1; d <= daysInMonth; d++) {
    const date = new Date(year, month - 1, d);
    const holiday = false; // TODO: kalendář svátků
    const weekend = isWeekend(date);
    const workday = isWorkday(date, holiday);

    const attendanceHours = workday ? STANDARD_WORKDAY_HOURS : 0;

    const projectHours = workday ? projects.reduce((sum, p) => sum + STANDARD_WORKDAY_HOURS * p.workload, 0) : 0;

    days.push({
      date,
      isHoliday: holiday,
      isWeekend: weekend,
      isWorkday: workday,

      attendanceHours,
      projectHours,

      attendanceWorkload: workload,
      projectWorkload: projects.reduce((s, p) => s + p.workload, 0),
    });
  }

  const totalHours = days.reduce((s, d) => s + d.attendanceHours + d.projectHours, 0);
  const totalWorkload = days.reduce((s, d) => s + d.attendanceWorkload + d.projectWorkload, 0);
  const totalHoursObligation = days.reduce((s, d) => s + hoursObligation(workload, d.date, d.isHoliday), 0);

  return {
    year,
    month,
    days,
    totalHours,
    totalWorkload,
    totalHoursObligation,
  };
};
