import { createContext, useContext } from "react";

const TimesheetWorkflowRefreshContext = createContext<(() => void) | null>(null);

export const TimesheetWorkflowRefreshProvider = TimesheetWorkflowRefreshContext.Provider;

export const useTimesheetWorkflowRefresh = () => {
  const refresh = useContext(TimesheetWorkflowRefreshContext);
  return refresh ?? (() => {});
};
