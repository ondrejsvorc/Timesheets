import { useImmer } from "use-immer";

export interface EmployeeTimesheetsFilterCriteria {
  year: number;
  months: number[] | null; // null means "all months", empty array means no months selected
  onlyUnapproved: boolean;
}

const CURRENT_YEAR = new Date().getFullYear();

const initialFilter: EmployeeTimesheetsFilterCriteria = {
  year: CURRENT_YEAR,
  months: null, // "all months" by default
  onlyUnapproved: true,
};

export const useEmployeeTimesheetsFilter = () => {
  const [filter, setFilter] = useImmer<EmployeeTimesheetsFilterCriteria>(initialFilter);
  return { filter, setFilter };
};
