/** Úvazek ve formuláři jako celé číslo v procentech; API očekává zlomek 0–1. */

export const isWholeWorkloadPercentInRange = (value: string, min: number, max: number): boolean => {
  const s = value.trim();
  if (!/^\d+$/.test(s)) return false;
  const n = Number(s);
  return Number.isInteger(n) && n >= min && n <= max;
};

export const workloadPercentToFraction = (value: string): number => {
  return Number(value.trim()) / 100;
};

export const workloadFractionToPercent = (fraction: number): string => String(Math.round(fraction * 100));
