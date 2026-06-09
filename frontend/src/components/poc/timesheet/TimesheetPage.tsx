import { Suspense, startTransition, useCallback, useEffect, useRef, useState } from "react";
import { Await, useAsyncValue, useLoaderData, useNavigate, useSearchParams } from "react-router";
import { useImmer } from "use-immer";
import { BackButton, FullscreenButton, SaveButton } from "@/components/shared/buttons/ActionButtons";
import { GenericSkeleton } from "@/components/shared/data/GenericSkeleton";
import { TimesheetStatusBadge } from "@/components/shared/data/TimesheetStatusBadge";
import { PageHeader, PageSubtitle, PageTitle } from "@/components/shared/layout/PageHeader";
import { SubPageHeader, SubPageTitle } from "@/components/shared/layout/SubPageHeader";
import { Button } from "@/components/ui/button";
import { Routes } from "@/constants/routes";
import { Texts } from "@/constants/texts";
import type { GetEmployeeResponse } from "@/features/employee/api/getEmployee";
import { cn } from "@/utils/cn";
import { resolveEmployeeTypeName } from "@/utils/resolveEmployeeTypeName";
import type { Timesheet, TimesheetDay } from "../Timesheet";
import type { GetCombinedTimesheetOverviewResponse } from "./api/getCombinedTimesheetOverview";
import { updateTimesheet } from "./api/updateTimesheet";
import { ChangeTimesheetStatusDialog } from "./ChangeTimesheetStatusDialog";
import { TimesheetComments } from "./comments/TimesheetComments";
import { TimesheetGrid } from "./grid/TimesheetGrid";
import { TimesheetsOverview } from "./TimesheetsOverview";

interface TimesheetPageLoaderData {
  employeePromise: Promise<GetEmployeeResponse>;
  overviewPromise: Promise<GetCombinedTimesheetOverviewResponse>;
  timesheetPromise: Promise<Timesheet>;
}

export const TimesheetPage = () => {
  const loaderData = useLoaderData() as TimesheetPageLoaderData;

  return (
    <>
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

const CombinedTimesheetSubHeader = () => {
  const overview = useAsyncValue() as GetCombinedTimesheetOverviewResponse;

  return (
    <SubPageHeader trailing={<TimesheetStatusBadge status={overview.status} />}>
      <SubPageTitle>{Texts.combinedTimesheet}</SubPageTitle>
    </SubPageHeader>
  );
};

const TimesheetPageContent = () => {
  const { overviewPromise } = useLoaderData() as TimesheetPageLoaderData;
  const initialTimesheet = useAsyncValue() as Timesheet;
  const [timesheet, setTimesheet] = useImmer<Timesheet>(initialTimesheet);
  const [searchParams] = useSearchParams();
  const lockActorEmployeeId = searchParams.get("employeeId") ?? "";
  const [isFullscreen, setIsFullscreen] = useState(false);
  const [showGrid, setShowGrid] = useState(false);
  const [showComments, setShowComments] = useState(false);
  const [isStatusDialogOpen, setIsStatusDialogOpen] = useState(false);
  const didMeasureStablePaintRef = useRef(false);

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
    [setTimesheet],
  );

  const handleToggleProjectLock = useCallback(
    (projectId: string) => {
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
    [setTimesheet, lockActorEmployeeId],
  );

  const handleClearAttendanceFields = useCallback(() => {
    setTimesheet((draft) => {
      draft.days.forEach((day) => {
        day.attendance.clockIn = "";
        day.attendance.clockOut = "";
        day.attendance.breakStart = "";
        day.attendance.breakEnd = "";
      });
    });
  }, [setTimesheet]);

  const handleSave = async (_event: React.MouseEvent<HTMLButtonElement>, signal: AbortSignal) => {
    await updateTimesheet(timesheet, signal);
  };

  useEffect(() => {
    if (!import.meta.env.DEV || didMeasureStablePaintRef.current) return;
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
  }, []);

  useEffect(() => {
    let firstRafId: number | undefined;
    let secondRafId: number | undefined;
    let commentsTimerId: number | undefined;

    firstRafId = window.requestAnimationFrame(() => {
      secondRafId = window.requestAnimationFrame(() => {
        setShowGrid(true);
        commentsTimerId = window.setTimeout(() => {
          setShowComments(true);
        }, 120);
      });
    });

    return () => {
      if (firstRafId !== undefined) window.cancelAnimationFrame(firstRafId);
      if (secondRafId !== undefined) window.cancelAnimationFrame(secondRafId);
      if (commentsTimerId !== undefined) window.clearTimeout(commentsTimerId);
    };
  }, []);

  return (
    <>
      <div className={cn(isFullscreen && "fixed inset-0 z-[60] flex flex-col overflow-hidden bg-background p-4 md:p-6")}>
        {!isFullscreen && (
          <Suspense fallback={<GenericSkeleton />}>
            <Await resolve={overviewPromise}>
              <CombinedTimesheetSubHeader />
            </Await>
          </Suspense>
        )}
        <div className={cn("mb-6 flex flex-wrap items-center justify-between gap-3", isFullscreen && "bg-background/95")}>
          <div className="flex flex-wrap items-center gap-3">
            <Button type="button" variant="outline" onClick={() => setIsStatusDialogOpen(true)}>
              {Texts.changeTimesheetStatus}
            </Button>
            <Button type="button" variant="outline" onClick={handleClearAttendanceFields}>
              {Texts.clearAttendanceEntryAndBreak}
            </Button>
          </div>
          <div className="flex flex-wrap items-center gap-3">
            <FullscreenButton onClick={() => setIsFullscreen((current) => !current)} isFullscreen={isFullscreen} />
            <SaveButton onClick={handleSave}>{Texts.saveChanges}</SaveButton>
          </div>
        </div>
        {showGrid ? (
          <TimesheetGrid
            timesheet={timesheet}
            onUpdateDay={handleUpdateDay}
            onToggleProjectLock={handleToggleProjectLock}
            className={isFullscreen ? "min-h-0 flex-1 max-h-none" : undefined}
          />
        ) : (
          <div className={cn(isFullscreen ? "min-h-0 flex-1" : "h-[420px]")}>
            <GenericSkeleton />
          </div>
        )}
      </div>
      {showComments && <TimesheetComments />}
      <ChangeTimesheetStatusDialog open={isStatusDialogOpen} onClose={() => setIsStatusDialogOpen(false)} />
    </>
  );
};
