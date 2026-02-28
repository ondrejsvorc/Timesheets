import { type FilterCriteria, useFilter } from "@/hooks/useFilter";
import type { EmployeeItem } from "../api/getContractEmployees";

export interface ContractEmployeesFilterCriteria extends FilterCriteria {}

const initialFilter: ContractEmployeesFilterCriteria = {
  query: "",
};

export const useContractEmployeesFilter = (items: EmployeeItem[]) =>
  useFilter<EmployeeItem, ContractEmployeesFilterCriteria>({
    items,
    initialFilter,
    keys: [
      (item) => item.fullName,
      (item) => item.personalNumber.toString(),
      (item) => item.positions.map((p) => p.position ?? "").join(" "),
    ],
  });
