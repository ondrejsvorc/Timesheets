import { useCallback, useEffect, useState } from "react";
import { Suspense, useAsyncValue, useLoaderData, useNavigate, useSearchParams } from "react-router";
import { useImmer } from "use-immer";
import { UiAction } from "@/auth/uiPermissions";
import { useCan } from "@/auth/useCan";
import { BackButton, FullscreenButton } from "@/components/shared/buttons/ActionButtons";
import { GenericSkeleton } from "@/components/shared/data/GenericSkeleton";
import { TimesheetStatusBadge } from "@/components/shared/data/TimesheetStatusBadge";
import { AwaitContent } from "@/components/shared/layout/AwaitContent";
import { PageHeader, PageSubtitle, PageTitle } from "@/components/shared/layout/PageHeader";
import { SubPageHeader, SubPageTitle } from "@/components/shared/layout/SubPageHeader";
import { Routes } from "@/constants/routes";
import { Texts } from "@/constants/texts";
import { formatMonthYear } from "@/features/contract/utils/czechMonths";
import type { GetEmployeeResponse } from "@/features/employee/api";
import { resolveEmployeeTypeName } from "@/features/employee/employeeType";
import { cn } from "@/utils/common";
import { formatWorkload } from "@/utils/format";
import { allocateTimesheet, type GetTimesheetOverviewResponse, reviewTimesheet, updateTimesheet } from "./api";
import type { TimesheetComment } from "./comments/Comment";
import { TimesheetComments } from "./comments/TimesheetComments";
import { TimesheetGrid } from "./grid/TimesheetGrid";
import type { Timesheet, TimesheetData, TimesheetDay, TimesheetEvaluation } from "./Timesheet";
import { TimesheetsOverview } from "./TimesheetsOverview";
import { TimesheetWorkflowToolbar } from "./TimesheetWorkflowToolbar";

export interface TimesheetLoaderData {
  employeePromise: Promise<GetEmployeeResponse>;
  overviewPromise: Promise<GetTimesheetOverviewResponse>;
  timesheetPromise: Promise<TimesheetData>;
  commentsPromise: Promise<TimesheetComment[]>;
}

export const TimesheetPage = () => {
  const loaderData = useLoaderData() as TimesheetLoaderData;

  return (
    <AwaitContent promise={loaderData.employeePromise}>
      <TimesheetPageContent loaderData={loaderData} />
    </AwaitContent>
  );
};

interface TimesheetPageContentProps {
  loaderData: TimesheetLoaderData;
}

const TimesheetPageContent = ({ loaderData }: TimesheetPageContentProps) => {
  const employee = useAsyncValue() as GetEmployeeResponse;
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const employeeId = searchParams.get("employeeId") ?? "";

  const employeeType = resolveEmployeeTypeName(employee.employee.employeeTypeId);

  return (
    <>
      <PageHeader leading={<BackButton onClick={() => navigate(Routes.employee(employee.employee.id))} />}>
        <PageTitle>{employee.employee.fullName}</PageTitle>
        <Suspense fallback={<PageSubtitle>{employeeType}</PageSubtitle>}>
          <AwaitContent promise={loaderData.overviewPromise}>
            <TimesheetPageSubtitle employeeType={employeeType} />
          </AwaitContent>
        </Suspense>
      </PageHeader>

      <AwaitContent promise={loaderData.overviewPromise}>
        <TimesheetOverviewSection />
      </AwaitContent>

      <AwaitContent promise={loaderData.timesheetPromise}>
        <TimesheetEditorSection overviewPromise={loaderData.overviewPromise} />
      </AwaitContent>

      {employeeId && (
        <AwaitContent promise={loaderData.commentsPromise}>
          <TimesheetCommentsSection employeeId={employeeId} overviewPromise={loaderData.overviewPromise} />
        </AwaitContent>
      )}
    </>
  );
};

const TimesheetPageSubtitle = ({ employeeType }: { employeeType: string }) => {
  const overview = useAsyncValue() as GetTimesheetOverviewResponse;
  const subtitleParts = [employeeType, `${formatMonthYear(overview.month, overview.year)} (${formatWorkload(overview.summary.totalWorkload)})`].filter((part) => part.length > 0);
  return <PageSubtitle>{subtitleParts.join(" · ")}</PageSubtitle>;
};

const TimesheetOverviewSection = () => {
  const overview = useAsyncValue() as GetTimesheetOverviewResponse;
  return <TimesheetsOverview overview={overview} />;
};

interface TimesheetEditorSectionProps {
  overviewPromise: Promise<GetTimesheetOverviewResponse>;
}

const TimesheetEditorSection = ({ overviewPromise }: TimesheetEditorSectionProps) => {
  const timesheetData = useAsyncValue() as TimesheetData;

  return (
    <Suspense fallback={<GenericSkeleton className="mt-6 min-h-96" />}>
      <AwaitContent promise={overviewPromise}>
        <TimesheetEditorWithOverview initialData={timesheetData} />
      </AwaitContent>
    </Suspense>
  );
};

const TimesheetEditorWithOverview = ({ initialData }: { initialData: TimesheetData }) => {
  const overview = useAsyncValue() as GetTimesheetOverviewResponse;
  return <TimesheetEditor initialData={initialData} overview={overview} />;
};

interface TimesheetCommentsSectionProps {
  employeeId: string;
  overviewPromise: Promise<GetTimesheetOverviewResponse>;
}

const TimesheetCommentsSection = ({ employeeId, overviewPromise }: TimesheetCommentsSectionProps) => {
  const comments = useAsyncValue() as TimesheetComment[];

  return (
    <Suspense fallback={null}>
      <AwaitContent promise={overviewPromise}>
        <TimesheetCommentsWithScope employeeId={employeeId} comments={comments} />
      </AwaitContent>
    </Suspense>
  );
};

const TimesheetCommentsWithScope = ({ employeeId, comments }: { employeeId: string; comments: TimesheetComment[] }) => {
  const overview = useAsyncValue() as GetTimesheetOverviewResponse;
  return <TimesheetComments scope={{ employeeId, year: overview.year, month: overview.month }} comments={comments} />;
};

interface TimesheetEditorProps {
  initialData: TimesheetData;
  overview: GetTimesheetOverviewResponse;
}

const TimesheetEditor = ({ initialData, overview }: TimesheetEditorProps) => {
  const [timesheet, setTimesheet] = useImmer<Timesheet>(initialData.timesheet);
  const [evaluation, setEvaluation] = useState<TimesheetEvaluation>(initialData.evaluation);
  const [searchParams] = useSearchParams();
  const timesheetEmployeeId = searchParams.get("employeeId") ?? "";
  const canEditTimesheet = useCan(UiAction.timesheet.edit, { employeeId: timesheetEmployeeId });
  const canSubmit = useCan(UiAction.timesheet.submit, { employeeId: timesheetEmployeeId });
  const canManageWhole = useCan(UiAction.timesheet.finalApprove, { employeeId: timesheetEmployeeId });
  const [isFullscreen, setIsFullscreen] = useState(false);

  const isDraft = overview.status === Texts.statusInProgress;
  const isSubmitted = overview.status === Texts.statusPendingApproval;
  const isApproved = overview.status === Texts.statusApproved;
  const isEditable = overview.status === Texts.statusInProgress && canEditTimesheet;
  const hasWorkflowButtons = (isDraft && canSubmit) || (isSubmitted && canManageWhole) || (isApproved && canManageWhole);

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
          target.attendance.clockIn = allocated.clockIn;
          target.attendance.clockOut = allocated.clockOut;
          target.attendance.breakStart = allocated.breakStart;
          target.attendance.breakEnd = allocated.breakEnd;
          target.coreHours = allocated.coreHours || null;
          target.contractPartCells = allocated.contractPartCells;
          target.attendanceAdjusted = allocated.attendanceAdjusted;
        });
      });
      setEvaluation(allocation.evaluation);
    },
    [setTimesheet, timesheet],
  );

  return (
    <div className={cn(isFullscreen && "fixed inset-0 z-[60] flex flex-col overflow-hidden bg-background px-4 pb-4 pt-2 md:px-6 md:pb-6 md:pt-2")}>
      <SubPageHeader
        className={isFullscreen ? "shrink-0 py-2" : undefined}
        actions={!hasWorkflowButtons && <FullscreenButton onClick={() => setIsFullscreen((current) => !current)} isFullscreen={isFullscreen} />}
      >
        <div className="flex items-center gap-2">
          <SubPageTitle>{Texts.timesheet}</SubPageTitle>
          <TimesheetStatusBadge status={overview.status} />
        </div>
      </SubPageHeader>
      {hasWorkflowButtons && (
        <TimesheetWorkflowToolbar timesheet={timesheet} overview={overview} isFullscreen={isFullscreen} onToggleFullscreen={() => setIsFullscreen((current) => !current)} onSave={handleSave} />
      )}
      <TimesheetGrid
        timesheet={timesheet}
        evaluation={evaluation}
        readOnly={!isEditable}
        onUpdateDay={handleUpdateDay}
        onAllocate={handleAllocate}
        className={isFullscreen ? "min-h-0 flex-1 max-h-none" : undefined}
      />
    </div>
  );
};
