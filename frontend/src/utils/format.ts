import { format, parseISO } from "date-fns";
import { cs } from "date-fns/locale";
import { Texts } from "@/constants/texts";

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
