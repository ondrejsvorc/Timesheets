import { type FilterCriteria, useFilter } from "@/hooks/useFilter";
import type { EmployeePositionItem } from "../api/getEmployeePositions";

export interface PositionsFilterCriteria extends FilterCriteria {}

const initialFilter: PositionsFilterCriteria = {
  query: "",
};

export const usePositionsFilter = (items: EmployeePositionItem[]) =>
  useFilter<EmployeePositionItem, PositionsFilterCriteria>({
    items,
    initialFilter,
    keys: [(item) => item.position, (item) => item.projectName, (item) => item.contractName, (item) => item.startDate, (item) => item.endDate ?? ""],
  });
