export function formatWorkloadPercent(value: unknown): string {
  if (value == null) {
    return "-";
  }

  const normalized = typeof value === "string" ? value.trim().replace("%", "").replace(",", ".") : value;

  const parsed = Number(normalized);
  if (!Number.isFinite(parsed)) {
    return "-";
  }

  // Supports both fractional input (0.25) and percent input (25).
  const fraction = parsed > 1 ? parsed / 100 : parsed;
  return `${Math.round(fraction * 100)} %`;
}
