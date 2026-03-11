import { Suspense, useState, type Dispatch, type SetStateAction } from "react";
import { Await, useAsyncValue, useLoaderData, useNavigate } from "react-router";
import { useImmer } from "use-immer";
import { BackButton, SaveButton } from "@/components/shared/buttons/ActionButtons";
import { GenericSkeleton } from "@/components/shared/data/GenericSkeleton";
import { PageHeader, PageSubtitle, PageTitle } from "@/components/shared/layout/PageHeader";
import { SubPageHeader, SubPageTitle } from "@/components/shared/layout/SubPageHeader";
import { Button } from "@/components/ui/button";
import { Routes } from "@/constants/routes";
import type { GetEmployeeResponse } from "@/features/employee/api/getEmployee";
import { resolveEmployeeTypeName } from "@/utils/resolveEmployeeTypeName";
import { cn } from "@/utils/cn";
import { Maximize2, Minimize2 } from "lucide-react";
import { EmployeeTimesheetsOverview } from "./EmployeeTimesheetsOverview";
import type { GetCombinedTimesheetOverviewResponse } from "./api/getCombinedTimesheetOverview";
import type { Timesheet, TimesheetDay } from "./Timesheet";
import { TimesheetTable } from "./TimesheetTable";

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
      {!isFullscreen && (
        <Suspense fallback={<GenericSkeleton />}>
          <Await resolve={loaderData.employeePromise}>
            <TimesheetPageHeader />
          </Await>
        </Suspense>
      )}
      {!isFullscreen && (
        <Suspense fallback={<GenericSkeleton />}>
          <Await resolve={loaderData.overviewPromise}>
            <EmployeeTimesheetsOverview />
          </Await>
        </Suspense>
      )}
      <Suspense fallback={<GenericSkeleton />}>
        <Await resolve={loaderData.timesheetPromise}>
          <TimesheetPageContent isFullscreen={isFullscreen} setIsFullscreen={setIsFullscreen} />
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

const TimesheetPageContent = ({
  isFullscreen,
  setIsFullscreen,
}: {
  isFullscreen: boolean;
  setIsFullscreen: Dispatch<SetStateAction<boolean>>;
}) => {
  const initialTimesheet = useAsyncValue() as Timesheet;
  const [timesheet, updateTimesheet] = useImmer<Timesheet>(initialTimesheet);

  const handleUpdateDay = (date: string, recipe: (day: TimesheetDay) => void) => {
    updateTimesheet((draft) => {
      const day = draft.days.find((dayInstance) => dayInstance.date === date);
      if (day) {
        recipe(day);
      }
    });
  };

  const handleSave = async (_event: React.MouseEvent<HTMLButtonElement>, _signal: AbortSignal) => {
    return Promise.resolve();
  };

  return (
    <div className={cn(isFullscreen && "fixed inset-0 z-[60] flex flex-col overflow-hidden bg-background p-4 md:p-6")}>
      {!isFullscreen && (
        <SubPageHeader>
          <SubPageTitle>Kombinovaný výkaz</SubPageTitle>
        </SubPageHeader>
      )}
      <div className={cn("mb-6 flex flex-wrap items-center justify-between gap-3", isFullscreen && "bg-background/95")}>
        <div className="flex flex-wrap items-center gap-3">
          <Button type="button" variant="outline">Upravit</Button>
          <Button type="button" variant="outline">Změnit stav výkazu</Button>
          <Button type="button" variant="outline">Exportovat</Button>
        </div>
        <div className="flex flex-wrap items-center gap-3">
          <Button type="button" variant="outline" onClick={() => setIsFullscreen((current) => !current)}>
            {isFullscreen ? <Minimize2 /> : <Maximize2 />}
            {isFullscreen ? "Ukončit fullscreen" : "Fullscreen"}
          </Button>
          <SaveButton onClick={handleSave}>Uložit změny</SaveButton>
        </div>
      </div>
      <TimesheetTable timesheet={timesheet} onUpdateDay={handleUpdateDay} className={isFullscreen ? "min-h-0 flex-1 max-h-none" : undefined} />
    </div>
  );
};
