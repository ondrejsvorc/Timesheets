import { useCallback, useRef, useState } from "react";
import type {
  ContractTimesheetsFilterCriteria,
  EmployeeItem,
  GetContractTimesheetsRequest,
  GetContractTimesheetsResponse,
  TimesheetItem,
} from "../api/getContractTimesheets";
import { getContractTimesheets, getDeltaMonths, monthInRange, rangeIsSubset, statusesEqual } from "../api/getContractTimesheets";

interface CacheEntry {
  fromYear: number;
  fromMonth: number;
  toYear: number;
  toMonth: number;
  statuses: string[] | undefined;
  employees: EmployeeItem[];
  timesheets: TimesheetItem[];
}

function filterDataToRange(
  employees: EmployeeItem[],
  timesheets: TimesheetItem[],
  fromYear: number,
  fromMonth: number,
  toYear: number,
  toMonth: number,
  statuses: string[] | undefined,
): GetContractTimesheetsResponse {
  const filtered = timesheets.filter((t) => {
    if (!monthInRange(t.year, t.month, fromYear, fromMonth, toYear, toMonth)) return false;
    if (statuses?.length && !statuses.includes(t.status)) return false;
    return true;
  });
  const ids = new Set(filtered.map((t) => t.employeeId));
  const emp = employees.filter((e) => ids.has(e.id));
  return { employees: emp, timesheets: filtered };
}

function mergeEmployees(a: EmployeeItem[], b: EmployeeItem[]): EmployeeItem[] {
  const byId = new Map<string, EmployeeItem>();
  a.forEach((e) => {
    byId.set(e.id, e);
  });
  b.forEach((e) => {
    byId.set(e.id, e);
  });
  return Array.from(byId.values());
}

function mergeTimesheets(
  cached: TimesheetItem[],
  incoming: TimesheetItem[],
  deltaFromYear: number,
  deltaFromMonth: number,
  deltaToYear: number,
  deltaToMonth: number,
): TimesheetItem[] {
  const outsideDelta = cached.filter((t) => !monthInRange(t.year, t.month, deltaFromYear, deltaFromMonth, deltaToYear, deltaToMonth));
  const byId = new Map<string, TimesheetItem>();
  outsideDelta.forEach((t) => {
    byId.set(t.id, t);
  });
  incoming.forEach((t) => {
    byId.set(t.id, t);
  });
  return Array.from(byId.values());
}

export function useContractTimesheets(projectId: string, contractId: string) {
  const [data, setData] = useState<GetContractTimesheetsResponse | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const cacheRef = useRef<Map<string, CacheEntry>>(new Map());

  const fetchTimesheets = useCallback(
    async (filter: ContractTimesheetsFilterCriteria) => {
      const req: GetContractTimesheetsRequest = {
        fromYear: filter.fromYear,
        fromMonth: filter.fromMonth,
        toYear: filter.toYear,
        toMonth: filter.toMonth,
        statuses: filter.statuses,
      };
      const cacheKey = contractId;
      const cached = cacheRef.current.get(cacheKey);

      if (cached && statusesEqual(cached.statuses, req.statuses)) {
        if (rangeIsSubset(req.fromYear, req.fromMonth, req.toYear, req.toMonth, cached.fromYear, cached.fromMonth, cached.toYear, cached.toMonth)) {
          const view = filterDataToRange(cached.employees, cached.timesheets, req.fromYear, req.fromMonth, req.toYear, req.toMonth, req.statuses);
          setData(view);
          return;
        }

        const delta = getDeltaMonths(
          req.fromYear,
          req.fromMonth,
          req.toYear,
          req.toMonth,
          cached.fromYear,
          cached.fromMonth,
          cached.toYear,
          cached.toMonth,
        );
        if (delta.length > 0) {
          const first = delta[0]!;
          const last = delta[delta.length - 1]!;
          const deltaFromYear = first.year;
          const deltaFromMonth = first.month;
          const deltaToYear = last.year;
          const deltaToMonth = last.month;
          setIsLoading(true);
          try {
            const res = await getContractTimesheets(projectId, contractId, {
              fromYear: deltaFromYear,
              fromMonth: deltaFromMonth,
              toYear: deltaToYear,
              toMonth: deltaToMonth,
              statuses: req.statuses,
            });
            const newEmployees = mergeEmployees(cached.employees, res.employees);
            const newTimesheets = mergeTimesheets(cached.timesheets, res.timesheets, deltaFromYear, deltaFromMonth, deltaToYear, deltaToMonth);
            const cacheStartBefore = cached.fromYear < req.fromYear || (cached.fromYear === req.fromYear && cached.fromMonth <= req.fromMonth);
            const newFromYear = cacheStartBefore ? cached.fromYear : req.fromYear;
            const newFromMonth = cacheStartBefore ? cached.fromMonth : req.fromMonth;
            const cacheEndAfter = cached.toYear > req.toYear || (cached.toYear === req.toYear && cached.toMonth >= req.toMonth);
            const newToYear = cacheEndAfter ? cached.toYear : req.toYear;
            const newToMonth = cacheEndAfter ? cached.toMonth : req.toMonth;
            const entry: CacheEntry = {
              fromYear: newFromYear,
              fromMonth: newFromMonth,
              toYear: newToYear,
              toMonth: newToMonth,
              statuses: cached.statuses,
              employees: newEmployees,
              timesheets: newTimesheets,
            };
            cacheRef.current.set(cacheKey, entry);
            const view = filterDataToRange(entry.employees, entry.timesheets, req.fromYear, req.fromMonth, req.toYear, req.toMonth, req.statuses);
            setData(view);
          } finally {
            setIsLoading(false);
          }
          return;
        }
      }

      setIsLoading(true);
      try {
        const res = await getContractTimesheets(projectId, contractId, req);
        cacheRef.current.set(cacheKey, {
          fromYear: req.fromYear,
          fromMonth: req.fromMonth,
          toYear: req.toYear,
          toMonth: req.toMonth,
          statuses: req.statuses,
          employees: res.employees,
          timesheets: res.timesheets,
        });
        setData(res);
      } finally {
        setIsLoading(false);
      }
    },
    [contractId, projectId],
  );

  return { data, isLoading, fetchTimesheets };
}
