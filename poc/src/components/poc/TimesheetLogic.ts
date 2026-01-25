import type { Attendance, TimesheetDay } from "./Timesheet";

/**
 * Converts "HH:mm" time format to minutes.
 */
const toMinutes = (time: string): number => {
  const [hours, minutes] = (time || "").split(":").map(Number);
  return (hours || 0) * 60 + (minutes || 0);
};

export const TimesheetLogic = {
  calculateWorkedHours: (attendance: Attendance): number => {
    const clockIn = toMinutes(attendance.clockIn);
    const clockOut = toMinutes(attendance.clockOut);
    if (!clockIn || !clockOut || clockOut <= clockIn) {
      return 0;
    }

    const breakStart = toMinutes(attendance.breakStart);
    const breakEnd = toMinutes(attendance.breakEnd);
    const breakMinutes = attendance.breakStart && attendance.breakEnd && breakEnd > breakStart ? breakEnd - breakStart : 0;

    const workedMinutes = clockOut - clockIn;
    const workedMinutesWithoutBreak = workedMinutes - breakMinutes;
    const workedHours = Number((workedMinutesWithoutBreak / 60).toFixed(2));

    return workedHours;
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

  getDelta: (day: TimesheetDay): number => {
    const worked = TimesheetLogic.calculateWorkedHours(day.attendance);
    const allocated = day.coreHours + Object.values(day.projectHours).reduce((sum, h) => sum + h, 0);
    return Number((allocated - worked).toFixed(2));
  },

  isValidTime: (time: string): boolean => {
    const regex = /^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$/;
    return regex.test(time);
  },

  formatSmartTime: (value: string): string => {
    const clean = value.replace(/\D/g, "");
    if (!clean) return "";

    let h = 0;
    let m = 0;

    if (clean.length <= 2) {
      h = parseInt(clean);
    } else {
      h = parseInt(clean.slice(0, -2));
      m = parseInt(clean.slice(-2));
    }

    h = Math.min(h, 23);
    m = Math.min(m, 59);

    return `${h.toString().padStart(2, "0")}:${m.toString().padStart(2, "0")}`;
  },

  getMonthlyTotalForProject: (days: TimesheetDay[], projectId: string) => {
    return days.reduce((sum, day) => sum + (day.projectHours[projectId] || 0), 0);
  },
};
