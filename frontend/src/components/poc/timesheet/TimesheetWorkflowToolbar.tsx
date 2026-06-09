import { Check, RotateCcw, Send } from "lucide-react";
import { useState } from "react";
import { useRevalidator, useSearchParams } from "react-router";
import { FullscreenButton, SaveButton, UnlockIcon } from "@/components/shared/buttons/ActionButtons";
import { Button } from "@/components/ui/button";
import { Texts } from "@/constants/texts";
import { TimesheetStatusIds } from "@/constants/timesheetStatuses";
import { formatMonthYear } from "@/features/contract/utils/czechMonths";
import type { Timesheet } from "../Timesheet";
import type { GetCombinedTimesheetOverviewResponse } from "./api/getCombinedTimesheetOverview";
import { updateCombinedTimesheetStatus } from "./api/updateCombinedTimesheetStatus";
import { type TimesheetWorkflowAction, TimesheetWorkflowConfirmDialog } from "./TimesheetWorkflowConfirmDialog";

interface TimesheetWorkflowToolbarProps {
  timesheet: Timesheet;
  overview: GetCombinedTimesheetOverviewResponse;
  isFullscreen: boolean;
  onToggleFullscreen: () => void;
  onSave: (signal: AbortSignal) => Promise<void>;
  onWorkflowSuccess: () => void;
  onClearAttendanceFields: () => void;
}

export const TimesheetWorkflowToolbar = ({
  timesheet,
  overview,
  isFullscreen,
  onToggleFullscreen,
  onSave,
  onWorkflowSuccess,
  onClearAttendanceFields,
}: TimesheetWorkflowToolbarProps) => {
  const [searchParams] = useSearchParams();
  const revalidator = useRevalidator();
  const [activeWorkflow, setActiveWorkflow] = useState<TimesheetWorkflowAction | null>(null);

  const employeeId = searchParams.get("employeeId") ?? "";
  const periodLabel = formatMonthYear(overview.month, overview.year);
  const isDraft = overview.status === Texts.statusInProgress;
  const isSubmitted = overview.status === Texts.statusPendingApproval;
  const isApproved = overview.status === Texts.statusApproved;

  const changeStatus = async (statusId: string, comment: string, signal: AbortSignal) => {
    await updateCombinedTimesheetStatus(
      {
        employeeId,
        year: overview.year,
        month: overview.month,
        statusId,
        comment,
        timesheetIds: [timesheet.id],
      },
      signal,
    );
    revalidator.revalidate();
    onWorkflowSuccess();
  };

  const handleWorkflowConfirm = async (comment: string, signal: AbortSignal) => {
    switch (activeWorkflow) {
      case "submit":
        await onSave(signal);
        await changeStatus(TimesheetStatusIds.submitted, comment, signal);
        break;
      case "approve":
        await changeStatus(TimesheetStatusIds.approved, comment, signal);
        break;
      case "return":
        await changeStatus(TimesheetStatusIds.draft, comment, signal);
        break;
      case "unlock":
        await changeStatus(TimesheetStatusIds.draft, comment, signal);
        break;
      default:
        break;
    }
  };

  return (
    <>
      <div className="mb-6 flex flex-wrap items-center justify-between gap-3">
        <div className="flex flex-wrap items-center gap-3">
          {isDraft && (
            <>
              <Button type="button" onClick={() => setActiveWorkflow("submit")}>
                <span className="inline-flex items-center gap-2">
                  <Send className="size-4" />
                  {Texts.submitForApproval}
                </span>
              </Button>
              <Button type="button" variant="outline" onClick={onClearAttendanceFields}>
                {Texts.clearAttendanceEntryAndBreak}
              </Button>
            </>
          )}
          {isSubmitted && (
            <>
              <Button type="button" onClick={() => setActiveWorkflow("approve")}>
                <span className="inline-flex items-center gap-2">
                  <Check className="size-4" />
                  {Texts.approveTimesheet}
                </span>
              </Button>
              <Button type="button" variant="outline" onClick={() => setActiveWorkflow("return")}>
                <span className="inline-flex items-center gap-2">
                  <RotateCcw className="size-4" />
                  {Texts.returnToDraft}
                </span>
              </Button>
            </>
          )}
          {isApproved && (
            <Button type="button" variant="outline" onClick={() => setActiveWorkflow("unlock")}>
              <span className="inline-flex items-center gap-2">
                <UnlockIcon />
                {Texts.unlockTimesheet}
              </span>
            </Button>
          )}
        </div>
        <div className="flex flex-wrap items-center gap-3">
          <FullscreenButton onClick={onToggleFullscreen} isFullscreen={isFullscreen} />
          {isDraft && <SaveButton onClick={(_, signal) => onSave(signal)}>{Texts.saveChanges}</SaveButton>}
        </div>
      </div>

      <TimesheetWorkflowConfirmDialog
        action={activeWorkflow}
        periodLabel={periodLabel}
        onClose={() => setActiveWorkflow(null)}
        onConfirm={handleWorkflowConfirm}
      />
    </>
  );
};
