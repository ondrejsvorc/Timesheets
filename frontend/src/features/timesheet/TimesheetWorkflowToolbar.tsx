import { Check, RotateCcw, Send } from "lucide-react";
import { useState } from "react";
import { useRevalidator, useSearchParams } from "react-router";
import { FullscreenButton, SaveButton, UnlockIcon } from "@/components/shared/buttons/ActionButtons";
import { MessageAlertDialog } from "@/components/shared/dialogs/MessageAlertDialog";
import { Button } from "@/components/ui/button";
import { Texts } from "@/constants/texts";
import { formatMonthYear } from "@/features/contract/utils/czechMonths";
import { cn } from "@/utils/common";
import { type GetTimesheetOverviewResponse, type TimesheetStatusAction, updateTimesheetStatus } from "./api";
import type { Timesheet, TimesheetEvaluation } from "./Timesheet";
import { type TimesheetWorkflowAction, TimesheetWorkflowConfirmDialog } from "./TimesheetWorkflowConfirmDialog";

interface TimesheetWorkflowToolbarProps {
  timesheet: Timesheet;
  overview: GetTimesheetOverviewResponse;
  isFullscreen: boolean;
  onToggleFullscreen: () => void;
  onSave: (signal: AbortSignal) => Promise<TimesheetEvaluation>;
}

export const TimesheetWorkflowToolbar = ({ timesheet, overview, isFullscreen, onToggleFullscreen, onSave }: TimesheetWorkflowToolbarProps) => {
  const [searchParams] = useSearchParams();
  const revalidator = useRevalidator();
  const [activeWorkflow, setActiveWorkflow] = useState<TimesheetWorkflowAction | null>(null);
  const [submitBlockedOpen, setSubmitBlockedOpen] = useState(false);

  const employeeId = searchParams.get("employeeId") ?? "";
  const periodLabel = formatMonthYear(overview.month, overview.year);
  const { actions } = overview;

  const changeAttendanceStatus = async (action: TimesheetStatusAction, comment: string, signal: AbortSignal, draft?: Timesheet) => {
    await updateTimesheetStatus(
      {
        employeeId,
        year: overview.year,
        month: overview.month,
        action,
        comment,
        timesheetIds: [timesheet.id],
        timesheet: draft,
      },
      signal,
    );
    revalidator.revalidate();
  };

  const handleSaveClick = async (signal: AbortSignal) => {
    await onSave(signal);
  };

  const handleWorkflowConfirm = async (comment: string, signal: AbortSignal) => {
    switch (activeWorkflow) {
      case "submit":
        try {
          await changeAttendanceStatus("submit", comment, signal, timesheet);
        } catch {
          setSubmitBlockedOpen(true);
          throw new Error(Texts.workflowSubmitBlockedDescription);
        }
        break;
      case "finalApprove":
        await changeAttendanceStatus("approve", comment, signal);
        break;
      case "returnWhole":
        await changeAttendanceStatus("return", comment, signal);
        break;
      case "unlock":
        await changeAttendanceStatus("return", comment, signal);
        break;
      default:
        break;
    }
  };

  return (
    <>
      <div className={cn("mb-6 flex flex-wrap items-center justify-between gap-3", isFullscreen && "mb-2 shrink-0")}>
        <div className="flex flex-wrap items-center gap-3">
          {actions.submit && (
            <Button type="button" onClick={() => setActiveWorkflow("submit")}>
              <span className="inline-flex items-center gap-2">
                <Send className="size-4" />
                {Texts.submitForApproval}
              </span>
            </Button>
          )}
          {actions.returnWhole && (
            <Button type="button" variant="outline" onClick={() => setActiveWorkflow("returnWhole")}>
              <span className="inline-flex items-center gap-2">
                <RotateCcw className="size-4" />
                {Texts.returnToDraft}
              </span>
            </Button>
          )}
          {actions.finalApprove && (
            <Button type="button" onClick={() => setActiveWorkflow("finalApprove")}>
              <span className="inline-flex items-center gap-2">
                <Check className="size-4" />
                {Texts.approveTimesheet}
              </span>
            </Button>
          )}
          {actions.unlock && (
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
          {actions.save && <SaveButton onClick={(_, signal) => handleSaveClick(signal)}>{Texts.saveChanges}</SaveButton>}
        </div>
      </div>
      <TimesheetWorkflowConfirmDialog action={activeWorkflow} periodLabel={periodLabel} onClose={() => setActiveWorkflow(null)} onConfirm={handleWorkflowConfirm} />
      <MessageAlertDialog open={submitBlockedOpen} title={Texts.workflowSubmitBlockedTitle} description={Texts.workflowSubmitBlockedDescription} onClose={() => setSubmitBlockedOpen(false)} />
    </>
  );
};
