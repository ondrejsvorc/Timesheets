import { createContext, useContext, useMemo } from "react";
import type { ReactNode } from "react";
import type { TimesheetDay } from "../../Timesheet";
import type { DayValidation } from "../../TimesheetValidations";
import { TimesheetValidations } from "../../TimesheetValidations";

const DayValidationsContext = createContext<DayValidation[]>([]);

interface DayValidationsProviderProps {
  day: TimesheetDay;
  previousDay?: TimesheetDay;
  children: ReactNode;
}

export const DayValidationsProvider = ({ day, previousDay, children }: DayValidationsProviderProps) => {
  const validations = useMemo(() => TimesheetValidations.validateDay(day, previousDay), [day, previousDay]);

  return (
    <DayValidationsContext.Provider value={validations}>
      {children}
    </DayValidationsContext.Provider>
  );
};

export const useDayValidations = () => useContext(DayValidationsContext);
