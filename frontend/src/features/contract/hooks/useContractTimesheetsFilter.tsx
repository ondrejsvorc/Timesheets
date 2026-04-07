import type { Draft } from "immer";
import { useEffect, useMemo, useRef } from "react";
import { useSearchParams } from "react-router";
import { useImmer } from "use-immer";
import type { ContractTimesheetsFilterCriteria } from "../api/getContractTimesheets";
import { buildTimesheetsRequestFromUrl } from "../api/getContractTimesheets";

export type { ContractTimesheetsFilterCriteria };

function filterToSearchParams(filter: ContractTimesheetsFilterCriteria): URLSearchParams {
  const next = new URLSearchParams();
  next.set("fromYear", String(filter.fromYear));
  next.set("fromMonth", String(filter.fromMonth));
  next.set("toYear", String(filter.toYear));
  next.set("toMonth", String(filter.toMonth));
  next.set("groupBy", filter.groupBy);
  if (filter.statuses?.length) {
    next.set("status", filter.statuses.join(","));
  }
  return next;
}

export function useContractTimesheetsFilter() {
  const [, setSearchParams] = useSearchParams();

  const initialFilter = useMemo<ContractTimesheetsFilterCriteria>(() => {
    return buildTimesheetsRequestFromUrl(new URL(window.location.href));
  }, []);

  const [filter, setFilter] = useImmer<ContractTimesheetsFilterCriteria>(initialFilter);
  const lastSyncedKey = useRef<string>("");

  useEffect(() => {
    const params = filterToSearchParams(filter);
    const key = params.toString();
    if (key !== lastSyncedKey.current) {
      lastSyncedKey.current = key;
      setSearchParams(params, { replace: true });
    }
  }, [filter, setSearchParams]);
  return { filter, setFilter };
}

export type SetContractTimesheetsFilter = (updater: (draft: Draft<ContractTimesheetsFilterCriteria>) => void) => void;
