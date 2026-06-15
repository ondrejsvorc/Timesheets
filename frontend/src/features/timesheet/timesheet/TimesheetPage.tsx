import { startTransition, useCallback, useEffect, useState } from "react";
import { useAsyncValue, useLoaderData, useNavigate, useRouteLoaderData, useSearchParams } from "react-router";
import { useImmer } from "use-immer";
import { UiAction } from "@/auth/uiPermissions";
import { useCan } from "@/auth/useCan";
import { BackButton } from "@/components/shared/buttons/ActionButtons";
import { TimesheetStatusBadge } from "@/components/shared/data/TimesheetStatusBadge";
import { AwaitContent } from "@/components/shared/layout/AwaitContent";
import { PageHeader, PageSubtitle, PageTitle } from "@/components/shared/layout/PageHeader";
import { SubPageHeader, SubPageTitle } from "@/components/shared/layout/SubPageHeader";
import { Routes } from "@/constants/routes";
import { Texts } from "@/constants/texts";
import { formatMonthYear } from "@/features/contract/utils/czechMonths";
import type { GetEmployeeResponse } from "@/features/employee/api/getEmployee";
import type { RootLoaderData } from "@/router";
import { cn } from "@/utils/cn";
import type { Timesheet, TimesheetData, TimesheetDay, TimesheetEvaluation } from "../Timesheet";
import { formatWorkload } from "../timesheetFormat";
import { allocateTimesheet } from "./api/allocateTimesheet";
import type { GetCombinedTimesheetOverviewResponse } from "./api/getCombinedTimesheetOverview";
import { reviewTimesheet } from "./api/reviewTimesheet";
import { updateTimesheet } from "./api/updateTimesheet";
import type { TimesheetComment } from "./comments/Comment";
import { TimesheetComments } from "./comments/TimesheetComments";
import { TimesheetGrid } from "./grid/TimesheetGrid";
import { TimesheetsOverview } from "./TimesheetsOverview";
import { TimesheetWorkflowToolbar } from "./TimesheetWorkflowToolbar";

export interface TimesheetPageData {
  employee: GetEmployeeResponse;
  overview: GetCombinedTimesheetOverviewResponse;
  timesheetData: TimesheetData;
  comments: TimesheetComment[];
}

export const TimesheetPage = () => {
  const { promise } = useLoaderData() as { promise: Promise<TimesheetPageData> };

  return (
    <AwaitContent promise={promise}>
      <TimesheetPageLoaded />
    </AwaitContent>
  );
};

const TimesheetPageLoaded = () => {
  const { employee, overview, timesheetData, comments } = useAsyncValue() as TimesheetPageData;
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const employeeId = searchParams.get("employeeId") ?? "";

  return (
    <>
      <PageHeader leading={<BackButton onClick={() => navigate(Routes.employee(employee.employee.id))} />}>
        <PageTitle>{employee.employee.fullName}</PageTitle>
        <PageSubtitle>
          {formatMonthYear(overview.month, overview.year)} ({formatWorkload(overview.summary.totalWorkload)})
        </PageSubtitle>
      </PageHeader>
      <TimesheetsOverview overview={overview} />
      <SubPageHeader>
        <div className="flex items-center gap-2">
          <SubPageTitle>{Texts.combinedTimesheet}</SubPageTitle>
          <TimesheetStatusBadge status={overview.status} />
        </div>
      </SubPageHeader>
      <TimesheetEditor key={timesheetData.timesheet.id} initialData={timesheetData} overview={overview} />
      {employeeId && <TimesheetComments scope={{ employeeId, year: overview.year, month: overview.month }} comments={comments} />}
    </>
  );
};

interface TimesheetEditorProps {
  initialData: TimesheetData;
  overview: GetCombinedTimesheetOverviewResponse;
}

const TimesheetEditor = ({ initialData, overview }: TimesheetEditorProps) => {
  const [timesheet, setTimesheet] = useImmer<Timesheet>(initialData.timesheet);
  const [evaluation, setEvaluation] = useState<TimesheetEvaluation>(initialData.evaluation);
  const [searchParams] = useSearchParams();
  const rootData = useRouteLoaderData("root") as RootLoaderData | undefined;
  const timesheetEmployeeId = searchParams.get("employeeId") ?? "";
  const lockActorEmployeeId = rootData?.currentUser?.id ?? timesheetEmployeeId;
  const canEditTimesheet = useCan(UiAction.timesheet.edit, { employeeId: timesheetEmployeeId });
  const [isFullscreen, setIsFullscreen] = useState(false);

  const isEditable = overview.status === Texts.statusInProgress && canEditTimesheet;

  useEffect(() => {
    const controller = new AbortController();
    const timeout = window.setTimeout(() => {
      reviewTimesheet(timesheet, controller.signal)
        .then(setEvaluation)
        .catch(() => {});
    }, 200);
    return () => {
      window.clearTimeout(timeout);
      controller.abort();
    };
  }, [timesheet]);

  const handleUpdateDay = useCallback(
    (dayIndex: number, updater: (day: TimesheetDay) => void) => {
      if (!isEditable) {
        return;
      }

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
      if (!isEditable) {
        return;
      }

      startTransition(() => {
        setTimesheet((draft) => {
          const project = draft.projects.find((p) => p.id === projectId);
          if (!project) {
            return;
          }
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
    if (!isEditable) {
      return;
    }

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
      const nextEvaluation = await updateTimesheet(timesheet, signal);
      setEvaluation(nextEvaluation);
      return nextEvaluation;
    },
    [timesheet],
  );

  const handleAllocate = useCallback(
    async (day?: number) => {
      const allocation = await allocateTimesheet(timesheet, day);
      setTimesheet((draft) => {
        allocation.days.forEach((allocated, index) => {
          const target = draft.days[index];
          if (!target) return;
          target.coreHours = allocated.coreHours || null;
          target.projectHours = allocated.projectHours;
        });
      });
      setEvaluation(allocation.evaluation);
    },
    [setTimesheet, timesheet],
  );

  return (
    <div className={cn(isFullscreen && "fixed inset-0 z-[60] flex flex-col overflow-hidden bg-background p-4 md:p-6")}>
      <TimesheetWorkflowToolbar
        timesheet={timesheet}
        evaluation={evaluation}
        overview={overview}
        isFullscreen={isFullscreen}
        onToggleFullscreen={() => setIsFullscreen((current) => !current)}
        onSave={handleSave}
        onClearAttendanceFields={handleClearAttendanceFields}
      />
      <TimesheetGrid
        timesheet={timesheet}
        evaluation={evaluation}
        readOnly={!isEditable}
        onUpdateDay={handleUpdateDay}
        onToggleProjectLock={handleToggleProjectLock}
        onAllocate={handleAllocate}
        className={isFullscreen ? "min-h-0 flex-1 max-h-none" : undefined}
      />
    </div>
  );
};
