import { Suspense, startTransition, useCallback, useMemo, useRef, useState } from "react";
import { Await, useAsyncValue, useLoaderData, useNavigate, useRevalidator, useRouteLoaderData, useSearchParams } from "react-router";
import { useImmer } from "use-immer";
import { UiAction } from "@/auth/uiPermissions";
import { useCan } from "@/auth/useCan";
import { BackButton } from "@/components/shared/buttons/ActionButtons";
import { GenericSkeleton } from "@/components/shared/data/GenericSkeleton";
import { TimesheetStatusBadge } from "@/components/shared/data/TimesheetStatusBadge";
import { PageHeader, PageSubtitle, PageTitle } from "@/components/shared/layout/PageHeader";
import { SubPageHeader, SubPageTitle } from "@/components/shared/layout/SubPageHeader";
import { Routes } from "@/constants/routes";
import { Texts } from "@/constants/texts";
import type { GetEmployeeResponse } from "@/features/employee/api/getEmployee";
import type { RootLoaderData } from "@/router";
import { cn } from "@/utils/cn";
import { resolveEmployeeTypeName } from "@/utils/resolveEmployeeTypeName";
import type { Timesheet, TimesheetDay } from "../Timesheet";
import type { GetCombinedTimesheetOverviewResponse } from "./api/getCombinedTimesheetOverview";
import { updateTimesheet } from "./api/updateTimesheet";
import type { TimesheetComment } from "./comments/Comment";
import { TimesheetComments } from "./comments/TimesheetComments";
import { TimesheetGrid } from "./grid/TimesheetGrid";
import { TimesheetsOverview } from "./TimesheetsOverview";
import { TimesheetWorkflowRefreshProvider } from "./TimesheetWorkflowRefreshContext";
import { TimesheetWorkflowToolbar } from "./TimesheetWorkflowToolbar";

interface TimesheetPageLoaderData {
  employeePromise: Promise<GetEmployeeResponse>;
  overviewPromise: Promise<GetCombinedTimesheetOverviewResponse>;
  timesheetPromise: Promise<Timesheet>;
  commentsPromise: Promise<TimesheetComment[]>;
}

export const TimesheetPage = () => {
  const loaderData = useLoaderData() as TimesheetPageLoaderData;
  const revalidator = useRevalidator();
  const onWorkflowSuccess = useMemo(() => () => revalidator.revalidate(), [revalidator]);
  const [searchParams] = useSearchParams();
  const employeeId = searchParams.get("employeeId") ?? "";
  const year = Number(searchParams.get("year"));
  const month = Number(searchParams.get("month"));
  const commentsScope = employeeId && Number.isInteger(year) && Number.isInteger(month) ? { employeeId, year, month } : null;

  return (
    <TimesheetWorkflowRefreshProvider value={onWorkflowSuccess}>
      <div>
        <Suspense fallback={<GenericSkeleton />}>
          <Await resolve={loaderData.employeePromise}>
            <TimesheetPageHeader />
          </Await>
        </Suspense>
      </div>
      <div>
        <Suspense fallback={<GenericSkeleton />}>
          <Await resolve={loaderData.overviewPromise}>
            <TimesheetsOverview />
          </Await>
        </Suspense>
      </div>
      <Suspense fallback={<GenericSkeleton />}>
        <Await resolve={loaderData.timesheetPromise}>
          <TimesheetPageContent />
        </Await>
      </Suspense>
      {commentsScope && (
        <Suspense fallback={<GenericSkeleton />}>
          <Await resolve={loaderData.commentsPromise}>
            <TimesheetComments scope={commentsScope} />
          </Await>
        </Suspense>
      )}
    </TimesheetWorkflowRefreshProvider>
  );
};

const TimesheetPageHeader = () => {
  const navigate = useNavigate();
  const response = useAsyncValue() as GetEmployeeResponse;
  const employee = response.employee;

  return (
    <PageHeader leading={<BackButton onClick={() => navigate(Routes.employee(employee.id))} />}>
      <PageTitle>{employee.fullName}</PageTitle>
      <PageSubtitle>
        {employee.personalNumber} · {employee.email} · {resolveEmployeeTypeName(employee.employeeTypeId)}
      </PageSubtitle>
    </PageHeader>
  );
};

const CombinedTimesheetSubHeader = () => {
  const overview = useAsyncValue() as GetCombinedTimesheetOverviewResponse;

  return (
    <SubPageHeader>
      <div className="flex items-center gap-2">
        <SubPageTitle>{Texts.combinedTimesheet}</SubPageTitle>
        <TimesheetStatusBadge status={overview.status} />
      </div>
    </SubPageHeader>
  );
};

const TimesheetPageContent = () => {
  const initialTimesheet = useAsyncValue() as Timesheet;
  const { overviewPromise } = useLoaderData() as TimesheetPageLoaderData;

  return (
    <Await resolve={overviewPromise}>
      <TimesheetEditor key={initialTimesheet.id} initialTimesheet={initialTimesheet} />
    </Await>
  );
};

interface TimesheetEditorProps {
  initialTimesheet: Timesheet;
}

const TimesheetEditor = ({ initialTimesheet }: TimesheetEditorProps) => {
  const overview = useAsyncValue() as GetCombinedTimesheetOverviewResponse;
  const [timesheet, setTimesheet] = useImmer<Timesheet>(initialTimesheet);
  const [searchParams] = useSearchParams();
  const rootData = useRouteLoaderData("root") as RootLoaderData | undefined;
  const timesheetEmployeeId = searchParams.get("employeeId") ?? "";
  const lockActorEmployeeId = rootData?.currentUser?.id ?? timesheetEmployeeId;
  const canEditTimesheet = useCan(UiAction.timesheet.edit, { employeeId: timesheetEmployeeId });
  const [isFullscreen, setIsFullscreen] = useState(false);
  const didMeasureStablePaintRef = useRef(false);
  const { overviewPromise } = useLoaderData() as TimesheetPageLoaderData;

  const isEditable = overview.status === Texts.statusInProgress && canEditTimesheet;

  const handleUpdateDay = useCallback(
    (dayIndex: number, updater: (day: TimesheetDay) => void) => {
      if (!isEditable) return;

      setTimesheet((draft) => {
        const day = draft.days[dayIndex];
        if (!day) {
          throw new RangeError(`Invalid day index: ${dayIndex}`);
        }
        updater(day);
      });
    },
    [isEditable, setTimesheet],
  );

  const handleToggleProjectLock = useCallback(
    (projectId: string) => {
      if (!isEditable) return;

      startTransition(() => {
        setTimesheet((draft) => {
          const project = draft.projects.find((p) => p.id === projectId);
          if (!project) return;
          if (project.lockedAt) {
            project.lockedAt = null;
            project.lockedBy = null;
          } else {
            project.lockedAt = new Date().toISOString();
            project.lockedBy = lockActorEmployeeId || null;
          }
        });
      });
    },
    [isEditable, setTimesheet, lockActorEmployeeId],
  );

  const handleClearAttendanceFields = useCallback(() => {
    if (!isEditable) return;

    setTimesheet((draft) => {
      draft.days.forEach((day) => {
        day.attendance.clockIn = "";
        day.attendance.clockOut = "";
        day.attendance.breakStart = "";
        day.attendance.breakEnd = "";
      });
    });
  }, [isEditable, setTimesheet]);

  const handleSave = useCallback(
    async (signal: AbortSignal) => {
      await updateTimesheet(timesheet, signal);
    },
    [timesheet],
  );

  if (import.meta.env.DEV && !didMeasureStablePaintRef.current) {
    didMeasureStablePaintRef.current = true;
    requestAnimationFrame(() => {
      requestAnimationFrame(() => {
        performance.mark("timesheet:first-stable-paint");
        const dataReadyMark = performance.getEntriesByName("timesheet:data-ready").at(-1);
        const stablePaintMark = performance.getEntriesByName("timesheet:first-stable-paint").at(-1);
        if (!dataReadyMark || !stablePaintMark) return;

        const durationMs = stablePaintMark.startTime - dataReadyMark.startTime;
        console.log(`[Timesheet] first stable paint after data ready: ${durationMs.toFixed(1)}ms`);
      });
    });
  }

  return (
    <div className={cn(isFullscreen && "fixed inset-0 z-[60] flex flex-col overflow-hidden bg-background p-4 md:p-6")}>
      {!isFullscreen && (
        <Suspense fallback={<GenericSkeleton />}>
          <Await resolve={overviewPromise}>
            <CombinedTimesheetSubHeader />
          </Await>
        </Suspense>
      )}
      <TimesheetWorkflowToolbar
        timesheet={timesheet}
        overview={overview}
        isFullscreen={isFullscreen}
        onToggleFullscreen={() => setIsFullscreen((current) => !current)}
        onSave={handleSave}
        onClearAttendanceFields={handleClearAttendanceFields}
      />
      <TimesheetGrid
        timesheet={timesheet}
        readOnly={!isEditable}
        onUpdateDay={handleUpdateDay}
        onToggleProjectLock={handleToggleProjectLock}
        className={isFullscreen ? "min-h-0 flex-1 max-h-none" : undefined}
      />
    </div>
  );
};
