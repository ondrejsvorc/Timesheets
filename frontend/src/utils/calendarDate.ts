import { format, parseISO } from "date-fns";

/** Calendar date picked in the UI → UTC midnight ISO for API/DB. */
export const formatCalendarDateForApi = (date: Date): string => `${format(date, "yyyy-MM-dd")}T00:00:00.000Z`;

/** API/form calendar date value → local Date for UI comparisons and pickers. */
export const parseCalendarDate = (value: string): Date => parseISO(value);
