/** Úvazek ve formuláři jako procento; API očekává zlomek 0–1. */

const normalizeWorkloadPercent = (value: string) => value.trim().replace(",", ".");

export const isWorkloadPercentInRange = (value: string, min: number, max: number): boolean => {
  const normalized = normalizeWorkloadPercent(value);
  if (!/^\d+(?:\.\d{1,2})?$/.test(normalized)) return false;
  const n = Number(normalized);
  return Number.isFinite(n) && n >= min && n <= max;
};

export const workloadPercentToFraction = (value: string): number => {
  return Number(normalizeWorkloadPercent(value)) / 100;
};

export const workloadFractionToPercent = (fraction: number): string =>
  Number((fraction * 100).toFixed(2))
    .toString()
    .replace(".", ",");
