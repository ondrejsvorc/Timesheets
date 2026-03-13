import { Suspense, useCallback, useState } from "react";
import { Await, useAsyncValue, useLoaderData, useNavigate } from "react-router";
import { useImmer } from "use-immer";
import { BackButton, FullscreenButton, SaveButton } from "@/components/shared/buttons/ActionButtons";
import { GenericSkeleton } from "@/components/shared/data/GenericSkeleton";
import { PageHeader, PageSubtitle, PageTitle } from "@/components/shared/layout/PageHeader";
import { SubPageHeader, SubPageTitle } from "@/components/shared/layout/SubPageHeader";
import { Button } from "@/components/ui/button";
import { Texts } from "@/constants/texts";
import { Routes } from "@/constants/routes";
import type { GetEmployeeResponse } from "@/features/employee/api/getEmployee";
import { resolveEmployeeTypeName } from "@/utils/resolveEmployeeTypeName";
import { cn } from "@/utils/cn";
import type { GetCombinedTimesheetOverviewResponse } from "./api/getCombinedTimesheetOverview";
import type { Timesheet, TimesheetDay } from "../Timesheet";
import { TimesheetGrid } from "./grid/TimesheetGrid";
import { TimesheetsOverview } from "./TimesheetsOverview";

interface TimesheetPageLoaderData {
  employeePromise: Promise<GetEmployeeResponse>;
  overviewPromise: Promise<GetCombinedTimesheetOverviewResponse>;
  timesheetPromise: Promise<Timesheet>;
}

export const TimesheetPage = () => {
  const loaderData = useLoaderData() as TimesheetPageLoaderData;
  const [isFullscreen, setIsFullscreen] = useState(false);

  return (
    <>
      <div className={cn(isFullscreen && "hidden")}>
        <Suspense fallback={<GenericSkeleton />}>
          <Await resolve={loaderData.employeePromise}>
            <TimesheetPageHeader />
          </Await>
        </Suspense>
      </div>
      <div className={cn(isFullscreen && "hidden")}>
        <Suspense fallback={<GenericSkeleton />}>
          <Await resolve={loaderData.overviewPromise}>
            <TimesheetsOverview />
          </Await>
        </Suspense>
      </div>
      <Suspense fallback={<GenericSkeleton />}>
        <Await resolve={loaderData.timesheetPromise}>
          <TimesheetPageContent isFullscreen={isFullscreen} onToggleFullscreen={() => setIsFullscreen((current) => !current)} />
        </Await>
      </Suspense>
    </>
  );
};

const TimesheetPageHeader = () => {
  const navigate = useNavigate();
  const response = useAsyncValue() as GetEmployeeResponse;
  const employee = response.employee;

  return (
    <PageHeader leading={<BackButton onClick={() => navigate(Routes.employeeTimesheets(employee.id))} />}>
      <PageTitle>{employee.fullName}</PageTitle>
      <PageSubtitle>
        {employee.personalNumber} · {employee.email} · {resolveEmployeeTypeName(employee.employeeTypeId)}
      </PageSubtitle>
    </PageHeader>
  );
};

const TimesheetPageContent = ({ isFullscreen, onToggleFullscreen }: { isFullscreen: boolean; onToggleFullscreen: () => void }) => {
  const initialTimesheet = useAsyncValue() as Timesheet;
  const [timesheet, setTimesheet] = useImmer<Timesheet>(initialTimesheet);

  const handleUpdateDay = useCallback(
    (dayIndex: number, updater: (day: TimesheetDay) => void) => {
      setTimesheet((draft) => {
        const day = draft.days[dayIndex];
        if (!day) {
          throw new RangeError(`Invalid day index: ${dayIndex}`);
        }
        updater(day);
      });
    },
    [setTimesheet]
  );

  console.log(timesheet);

  const handleSave = async (_event: React.MouseEvent<HTMLButtonElement>, _signal: AbortSignal) => {
    // TODO: Implement save
    return Promise.resolve();
  };

  return (
    <div className={cn(isFullscreen && "fixed inset-0 z-[60] flex flex-col overflow-hidden bg-background p-4 md:p-6")}>
      {!isFullscreen && (
        <SubPageHeader>
          <SubPageTitle>{Texts.combinedTimesheet}</SubPageTitle>
        </SubPageHeader>
      )}
      <div className={cn("mb-6 flex flex-wrap items-center justify-between gap-3", isFullscreen && "bg-background/95")}>
        <div className="flex flex-wrap items-center gap-3">
          <Button type="button" variant="outline">{Texts.edit}</Button>
          <Button type="button" variant="outline">{Texts.changeTimesheetStatus}</Button>
          <Button type="button" variant="outline">{Texts.export}</Button>
        </div>
        <div className="flex flex-wrap items-center gap-3">
          <FullscreenButton onClick={onToggleFullscreen} isFullscreen={isFullscreen} />
          <SaveButton onClick={handleSave}>{Texts.saveChanges}</SaveButton>
        </div>
      </div>
      <TimesheetGrid timesheet={timesheet} onUpdateDay={handleUpdateDay} className={isFullscreen ? "min-h-0 flex-1 max-h-none" : undefined} />
    </div>
  );
};
