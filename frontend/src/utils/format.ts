import { addDays, format, max as maxDate, min as minDate, parseISO, startOfDay, subDays } from "date-fns";
import { cs } from "date-fns/locale";
import { Texts } from "@/constants/texts";

export const DATE_DISPLAY_PATTERN = /^\d{1,2}\. \d{1,2}\. \d{4}$/;

export const toDateOnlyIso = (date: Date): string => `${format(date, "yyyy-MM-dd")}T00:00:00.000Z`;
export const fromDateOnlyIso = (value: string): Date => {
  const [year, month, day] = value.slice(0, 10).split("-").map(Number);
  return new Date(year, month - 1, day);
};

export const formatDate = (iso: string | null | undefined): string => {
  if (!iso) return Texts.dash;
  try {
    return format(parseISO(iso), "d. M. yyyy", { locale: cs });
  } catch {
    return Texts.dash;
  }
};

export const formatDateDisplay = (iso: string | null | undefined): string => {
  if (!iso) return "";
  try {
    return format(parseISO(iso), "d. M. yyyy", { locale: cs });
  } catch {
    return "";
  }
};

export const padDateDisplay = (text: string): string => {
  const date = parseDateDisplay(text);
  if (date) return formatDateDisplay(toDateOnlyIso(date));
  return text;
};

export const isRealCalendarDate = (day: number, month: number, year: number): boolean => {
  if (month < 1 || month > 12 || day < 1 || year < 1 || year > 9999) return false;
  const date = new Date(year, month - 1, day);
  return date.getFullYear() === year && date.getMonth() === month - 1 && date.getDate() === day;
};

export const parseDateDisplay = (text: string): Date | null => {
  const match = text.trim().match(/^(\d{1,2})\.\s*(\d{1,2})\.\s*(\d{4})$/);
  if (!match) return null;
  const day = Number(match[1]);
  const month = Number(match[2]);
  const year = Number(match[3]);
  if (!isRealCalendarDate(day, month, year)) return null;
  return new Date(year, month - 1, day);
};

export const isDateInRange = (date: Date, min?: Date, max?: Date): boolean => {
  const value = startOfDay(date).getTime();
  if (min && value < startOfDay(min).getTime()) return false;
  if (max && value > startOfDay(max).getTime()) return false;
  return true;
};

export const dateFieldBounds = (field: "startDate" | "endDate", opts: { projectStart?: Date; projectEnd?: Date; startDate?: string; endDate?: string }): { min?: Date; max?: Date } => {
  const { projectStart, projectEnd, startDate, endDate } = opts;

  if (field === "startDate") {
    const maxCandidates = [projectEnd, endDate ? subDays(startOfDay(fromDateOnlyIso(endDate)), 1) : undefined].filter((d): d is Date => d !== undefined);
    return { min: projectStart, max: maxCandidates.length > 0 ? minDate(maxCandidates) : undefined };
  }

  const minCandidates = [projectStart, startDate ? addDays(startOfDay(fromDateOnlyIso(startDate)), 1) : undefined].filter((d): d is Date => d !== undefined);
  return { min: minCandidates.length > 0 ? maxDate(minCandidates) : undefined, max: projectEnd };
};

const formatPercentFromFraction = (fraction: number): string =>
  Number((fraction * 100).toFixed(2))
    .toString()
    .replace(".", ",");

const normalizeWorkloadPercentInput = (value: string) => value.trim().replace(",", ".");

export const formatWorkloadPercent = (value: unknown): string => {
  if (value == null) {
    return "-";
  }
  const normalized = typeof value === "string" ? value.trim().replace("%", "").replace(",", ".") : value;
  const parsed = Number(normalized);
  if (!Number.isFinite(parsed)) {
    return "-";
  }
  const fraction = parsed > 1 ? parsed / 100 : parsed;
  return `${formatPercentFromFraction(fraction)} %`;
};

export const isWorkloadPercentInRange = (value: string, min: number, max: number): boolean => {
  const normalized = normalizeWorkloadPercentInput(value);
  if (!/^\d+(?:\.\d{1,2})?$/.test(normalized)) return false;
  const n = Number(normalized);
  return Number.isFinite(n) && n >= min && n <= max;
};

export const workloadPercentToFraction = (value: string): number => Number(normalizeWorkloadPercentInput(value)) / 100;
export const workloadFractionToPercent = (fraction: number): string => formatPercentFromFraction(fraction);
export const formatWorkload = (fraction: number): string => `${workloadFractionToPercent(fraction)} %`;

export const formatHours = (value: number): string => {
  const rounded = Number(value.toFixed(2));
  return (Object.is(rounded, -0) ? 0 : rounded).toString().replace(".", ",");
};
