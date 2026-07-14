import type { EmployeeTimesheetsFilterCriteria, GetEmployeeTimesheetsResponse } from "../api";

function getAvailableMonthsForYear(availableMonths: number[], _year: number) {
  return availableMonths;
}

function getFallbackYear(availableYears: number[], selectedYear: number) {
  if (availableYears.length === 0 || availableYears.includes(selectedYear)) {
    return null;
  }

  const currentYear = new Date().getFullYear();
  if (availableYears.includes(currentYear)) {
    return currentYear;
  }

  const lastAvailableYear = availableYears[availableYears.length - 1];
  return lastAvailableYear ?? null;
}

export const normalizeEmployeeTimesheetsFilter = (filter: EmployeeTimesheetsFilterCriteria, response: GetEmployeeTimesheetsResponse): EmployeeTimesheetsFilterCriteria => {
  const fallbackYear = getFallbackYear(response.availableYears, filter.year);
  const year = fallbackYear ?? filter.year;
  const nextAvailableMonthSet = new Set(getAvailableMonthsForYear(response.availableMonths, year));

  if (filter.months === null) {
    if (fallbackYear !== null) {
      return { ...filter, year: fallbackYear };
    }
    return filter;
  }

  const nextMonths = filter.months.filter((month) => nextAvailableMonthSet.has(month));
  const monthsChanged = nextMonths.length !== filter.months.length;

  if (fallbackYear !== null || monthsChanged) {
    return {
      ...filter,
      year: fallbackYear ?? filter.year,
      months: monthsChanged ? (nextMonths.length > 0 ? nextMonths : null) : filter.months,
    };
  }

  return filter;
};
