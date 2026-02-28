export const CZECH_MONTH_NAMES: Record<number, string> = {
  1: "leden",
  2: "únor",
  3: "březen",
  4: "duben",
  5: "květen",
  6: "červen",
  7: "červenec",
  8: "srpen",
  9: "září",
  10: "říjen",
  11: "listopad",
  12: "prosinec",
};

export function formatMonthYear(month: number, year: number): string {
  return `${CZECH_MONTH_NAMES[month] ?? month} ${year}`;
}
