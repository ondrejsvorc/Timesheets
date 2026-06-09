import type { Attendance, ProjectDefinition, TimeRange, Timesheet, TimesheetDay } from "./Timesheet";
import { generateTimesheetData } from "./TimesheetGenerator";

const HOURS_PRECISION = 2;
const MAX_WORK_SHIFT_HOURS = 12;

/** Kódy přerušení — služební cesty se nealokují automaticky do kmene/projektů. */
const BUSINESS_TRIP_INTERRUPTION_CODES = new Set(["SCP", "SCS", "SCT", "SCZ", "SCZE", "SCZP", "SCZS"]);
const CORE_INTERRUPTION_CODES = new Set(["M"]);

const parseInterruptionCodes = (raw: string): string[] => {
  if (!raw.trim()) return [];
  return raw
    .split(",")
    .map((c) => c.trim().toUpperCase())
    .filter(Boolean);
};

const attendanceHasBusinessTripInterruption = (attendance: Attendance): boolean => {
  return parseInterruptionCodes(attendance.interruptions).some((code) => BUSINESS_TRIP_INTERRUPTION_CODES.has(code));
};

const attendanceHasCoreOnlyInterruption = (attendance: Attendance): boolean => {
  return parseInterruptionCodes(attendance.interruptions).some((code) => CORE_INTERRUPTION_CODES.has(code) || code.startsWith("N"));
};

/**
 * Konvertuje časový formát "HH:mm" na minuty.
 */
const toMinutes = (time: string): number => {
  const [hours, minutes] = (time || "").split(":").map(Number);
  return (hours || 0) * 60 + (minutes || 0);
};

const roundHours = (value: number): number => Number(value.toFixed(HOURS_PRECISION));
const hasAttendanceFilled = (attendance: Attendance): boolean => Boolean(attendance.clockIn || attendance.clockOut);
const GENERATOR_CONFIG = { roundingStep: 0.01, defaultDailyWorkHours: 8 } as const;

const cloneTimesheet = (timesheet: Timesheet): Timesheet => ({
  ...timesheet,
  projects: timesheet.projects.map((p) => ({ ...p })),
  days: timesheet.days.map((d) => ({
    ...d,
    attendance: { ...d.attendance, schedules: d.attendance.schedules.map((s) => ({ ...s })) },
    projectHours: { ...d.projectHours },
  })),
});

export const TimesheetLogic = {
  /**
   * Celkový čas docházky (příchod–odchod mínus přestávka). Zahrnuje i práci v noční době — nesnižuje se o sloupec „Noční práce“.
   */
  calculateAttendanceTotalHours: (attendance: Attendance): number => {
    return TimesheetLogic.calculateWorkedHours(attendance);
  },

  calculateWorkedHours: (attendance: Attendance): number => {
    if (!attendance.clockIn || !attendance.clockOut) {
      return 0;
    }

    const clockIn = toMinutes(attendance.clockIn);
    const clockOut = toMinutes(attendance.clockOut);

    let actualClockOut = clockOut;
    if (clockOut < clockIn) {
      actualClockOut = clockOut + 24 * 60;
      const workedMinutes = actualClockOut - clockIn;
      if (workedMinutes > 12 * 60) return 0;
    }

    if (actualClockOut <= clockIn) return 0;

    const workedMinutes = actualClockOut - clockIn;

    let breakMinutes = 0;
    if (attendance.breakStart && attendance.breakEnd) {
      const bStart = toMinutes(attendance.breakStart);
      const bEnd = toMinutes(attendance.breakEnd);

      if (bEnd < bStart) {
        const breakDuration = bEnd + 24 * 60 - bStart;
        if (breakDuration <= 12 * 60) breakMinutes = breakDuration;
      } else if (bEnd > bStart) {
        breakMinutes = bEnd - bStart;
      }
    }

    const workedMinutesWithoutBreak = workedMinutes - breakMinutes;
    return roundHours(workedMinutesWithoutBreak / 60);
  },

  formatWorkedHoursToHuman: (hours: number): string => {
    if (hours <= 0) return "0";
    const totalMinutes = Math.round(hours * 60);
    const wholeHours = Math.floor(totalMinutes / 60);
    const remainingMinutes = totalMinutes % 60;
    const parts: string[] = [];
    if (wholeHours > 0) parts.push(`${wholeHours}h`);
    if (remainingMinutes > 0) parts.push(`${remainingMinutes}m`);
    return parts.join(" ") || "0";
  },

  calculateNightHours: (attendance: Attendance): number => {
    const overlap = (a1: number, a2: number, b1: number, b2: number): number => {
      const start = Math.max(a1, b1);
      const end = Math.min(a2, b2);
      return Math.max(0, end - start);
    };

    if (!attendance.clockIn || !attendance.clockOut) return 0;

    const clockInMinutes = toMinutes(attendance.clockIn);
    const clockOutMinutes = toMinutes(attendance.clockOut);
    const crossesMidnight = clockOutMinutes < clockInMinutes;
    const shiftStart = clockInMinutes;
    const shiftEnd = crossesMidnight ? clockOutMinutes + 1440 : clockOutMinutes;

    const nightSegments: Array<[number, number]> = [
      [22 * 60, 24 * 60],
      [0, 6 * 60],
      [1440 + 22 * 60, 1440 + 24 * 60],
      [1440 + 0, 1440 + 6 * 60],
    ];

    const shiftNightMinutes = nightSegments.reduce((sum, [nStart, nEnd]) => sum + overlap(shiftStart, shiftEnd, nStart, nEnd), 0);

    let breakNightMinutes = 0;
    if (attendance.breakStart && attendance.breakEnd) {
      let bStart = toMinutes(attendance.breakStart);
      let bEnd = toMinutes(attendance.breakEnd);
      if (bEnd < bStart) bEnd += 1440;
      if (crossesMidnight && bStart < shiftStart) {
        bStart += 1440;
        bEnd += 1440;
      }
      breakNightMinutes = nightSegments.reduce((sum, [nStart, nEnd]) => sum + overlap(bStart, bEnd, nStart, nEnd), 0);
    }

    return roundHours(Math.max(0, shiftNightMinutes - breakNightMinutes) / 60);
  },

  calculateSchedulesTotal: (schedules: TimeRange[]): number => {
    if (!schedules || schedules.length === 0) return 0;
    const totalMinutes = schedules.reduce((acc, range) => {
      const start = toMinutes(range.start);
      const end = toMinutes(range.end);
      return range.start && range.end && end > start ? acc + (end - start) : acc;
    }, 0);
    return roundHours(totalMinutes / 60);
  },

  calculateInterruptionCoreHours: (day: TimesheetDay, totalWorkload: number): number => {
    const attendanceWorked = TimesheetLogic.calculateWorkedHours(day.attendance);
    if (attendanceWorked > 0 && attendanceWorked <= MAX_WORK_SHIFT_HOURS) {
      return roundHours(Math.min(MAX_WORK_SHIFT_HOURS, attendanceWorked));
    }

    const standard = 8 * totalWorkload;
    return roundHours(Math.min(MAX_WORK_SHIFT_HOURS, Math.max(0, standard)));
  },

  hasBusinessTripInterruption: (attendance: Attendance): boolean => attendanceHasBusinessTripInterruption(attendance),

  hasCoreOnlyInterruption: (attendance: Attendance): boolean => attendanceHasCoreOnlyInterruption(attendance),

  calculateMonthlyFund: (timesheet: Timesheet): number => {
    const workingDaysCount = timesheet.days.filter((day) => !day.isWeekend && !day.isHoliday).length;
    return roundHours(workingDaysCount * 8 * timesheet.totalWorkload);
  },

  /**
   * Navrácená funkce pro výpočet fondu konkrétního úvazku (kmen/projekt)
   */
  calculateWorkloadFund: (timesheet: Timesheet, workload: number | null | undefined): number => {
    if (workload == null) return 0;
    const workingDaysCount = timesheet.days.filter((day) => !day.isWeekend && !day.isHoliday).length;
    const standardDayHours = 8;
    return roundHours(workingDaysCount * standardDayHours * workload);
  },

  calculateMonthlyTotalWorked: (days: TimesheetDay[]): number => {
    return roundHours(days.reduce((sum, day) => sum + TimesheetLogic.calculateWorkedHours(day.attendance), 0));
  },

  calculateMonthlyTotalAllocated: (days: TimesheetDay[]): number => {
    return roundHours(days.reduce((sum, day) => sum + TimesheetLogic.calculateControlTotal(day), 0));
  },

  distributeRemainingHours: (
    day: TimesheetDay,
    totalWorkload: number,
    coreWorkload: number,
    projects: Pick<ProjectDefinition, "id" | "workload" | "lockedAt">[],
  ) => {
    if (attendanceHasBusinessTripInterruption(day.attendance)) {
      return null;
    }

    const synthetic: Timesheet = {
      id: "",
      year: 2000,
      month: 1,
      totalWorkload,
      hasBaseWorkload: true,
      core: { workload: coreWorkload },
      projects: projects.map((p) => ({
        id: p.id,
        registrationNumber: "",
        name: "",
        position: "",
        workload: p.workload,
        lockedAt: p.lockedAt ?? null,
        lockedBy: null,
      })),
      days: [{ ...day, attendance: { ...day.attendance, schedules: [...day.attendance.schedules] }, projectHours: { ...day.projectHours } }],
    };

    generateTimesheetData(synthetic, GENERATOR_CONFIG);
    const generatedDay = synthetic.days[0];
    if (!generatedDay) return null;

    const coreDelta = roundHours((generatedDay.coreHours ?? 0) - (day.coreHours ?? 0));
    const projectDeltas = projects
      .map((p) => ({
        id: p.id,
        delta: roundHours((generatedDay.projectHours[p.id] ?? 0) - (day.projectHours[p.id] ?? 0)),
        lockedAt: p.lockedAt,
      }))
      .filter((x) => !x.lockedAt && x.delta !== 0);

    if (coreDelta === 0 && projectDeltas.length === 0) {
      return null;
    }

    return (onUpdate: (recipe: (draftDay: TimesheetDay) => void) => void) => {
      onUpdate((draft) => {
        if (coreDelta !== 0) {
          draft.coreHours = roundHours(Math.max(0, (draft.coreHours ?? 0) + coreDelta));
        }
        projectDeltas.forEach(({ id, delta }) => {
          draft.projectHours[id] = roundHours(Math.max(0, (draft.projectHours[id] ?? 0) + delta));
        });
      });
    };
  },

  distributeMonthlyHours: (timesheet: Timesheet, onUpdateDay: (date: string, recipe: (draftDay: TimesheetDay) => void) => void) => {
    const generated = cloneTimesheet(timesheet);
    generateTimesheetData(generated, GENERATOR_CONFIG);

    generated.days.forEach((nextDay, index) => {
      const prevDay = timesheet.days[index];
      if (!prevDay) return;

      const coreDelta = roundHours((nextDay.coreHours ?? 0) - (prevDay.coreHours ?? 0));
      const projectDeltas = generated.projects
        .map((p) => ({
          id: p.id,
          delta: roundHours((nextDay.projectHours[p.id] ?? 0) - (prevDay.projectHours[p.id] ?? 0)),
          lockedAt: p.lockedAt,
        }))
        .filter((x) => !x.lockedAt && x.delta !== 0);

      if (coreDelta === 0 && projectDeltas.length === 0) {
        return;
      }

      onUpdateDay(prevDay.date, (draft: TimesheetDay) => {
        if (coreDelta !== 0) {
          draft.coreHours = roundHours(Math.max(0, (draft.coreHours ?? 0) + coreDelta));
        }
        projectDeltas.forEach(({ id, delta }) => {
          draft.projectHours[id] = roundHours(Math.max(0, (draft.projectHours[id] ?? 0) + delta));
        });
      });
    });
  },

  getDelta: (day: TimesheetDay): number => {
    if (!hasAttendanceFilled(day.attendance)) {
      return 0;
    }
    const worked = TimesheetLogic.calculateWorkedHours(day.attendance);
    if (worked > MAX_WORK_SHIFT_HOURS) {
      return 0;
    }
    const allocated = TimesheetLogic.calculateControlTotal(day);
    return roundHours(worked - allocated);
  },

  calculateControlTotal: (day: TimesheetDay): number => {
    const core = day.coreHours ?? 0;
    const projects = Object.values(day.projectHours).reduce((sum, h) => sum + (h || 0), 0);
    return roundHours(core + projects);
  },

  getDayTotals: (day: TimesheetDay): { workedHours: number; nightHours: number; stagHours: number; controlTotal: number; balance: number } => {
    const workedHours = TimesheetLogic.calculateAttendanceTotalHours(day.attendance);
    const nightRaw = TimesheetLogic.calculateNightHours(day.attendance);
    const nightHours = roundHours(Math.min(nightRaw, workedHours));
    const stagHours = TimesheetLogic.calculateSchedulesTotal(day.attendance.schedules);
    const controlTotal = TimesheetLogic.calculateControlTotal(day);
    const balance = hasAttendanceFilled(day.attendance) && workedHours <= MAX_WORK_SHIFT_HOURS ? roundHours(workedHours - controlTotal) : 0;
    return { workedHours, nightHours, stagHours, controlTotal, balance };
  },

  isValidTime: (time: string): boolean => /^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$/.test(time),

  formatSmartTime: (value: string): string => {
    const clean = value.replace(/\D/g, "");
    if (!clean) return "";
    let h = clean.length <= 2 ? parseInt(clean, 10) : parseInt(clean.slice(0, -2), 10);
    let m = clean.length <= 2 ? 0 : parseInt(clean.slice(-2), 10);
    h = Math.min(h, 23);
    m = Math.min(m, 59);
    return `${h.toString().padStart(2, "0")}:${m.toString().padStart(2, "0")}`;
  },

  formatHours: (value: number): string => {
    const rounded = roundHours(value);
    const normalized = Object.is(rounded, -0) ? 0 : rounded;
    const trimmed = normalized.toFixed(HOURS_PRECISION).replace(/\.?0+$/, "");
    return trimmed.replace(".", ",");
  },

  getMonthlyTotalForProject: (days: TimesheetDay[], projectId: string) => {
    return roundHours(days.reduce((sum, day) => sum + (day.projectHours[projectId] || 0), 0));
  },
};
