import { type FilterCriteria, useFilter } from "@/hooks/useFilter";
import type { EmployeeItem } from "../api/getEmployees";

export interface EmployeesFilterCriteria extends FilterCriteria {}

const initialFilter: EmployeesFilterCriteria = {
  query: "",
};

export const useEmployeesFilter = (items: EmployeeItem[]) =>
  useFilter<EmployeeItem, EmployeesFilterCriteria>({
    items,
    initialFilter,
    keys: [(item) => item.fullName, (item) => item.personalNumber ?? "", (item) => item.email?.toString() ?? ""],
  });
