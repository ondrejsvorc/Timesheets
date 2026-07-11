import { Check, RotateCcw, Send } from "lucide-react";
import { useState } from "react";
import { useRevalidator, useSearchParams } from "react-router";
import { UiAction } from "@/auth/uiPermissions";
import { useCan } from "@/auth/useCan";
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
  const [submitBlockedEvaluation, setSubmitBlockedEvaluation] = useState<TimesheetEvaluation | null>(null);

  const employeeId = searchParams.get("employeeId") ?? "";
  const periodLabel = formatMonthYear(overview.month, overview.year);
  const isDraft = overview.status === Texts.statusInProgress;
  const isSubmitted = overview.status === Texts.statusPendingApproval;
  const isApproved = overview.status === Texts.statusApproved;
  const contractPartItems = overview.items.filter((item) => item.kind === "contractPart");
  const allContractPartsApproved = contractPartItems.length === 0 || contractPartItems.every((item) => item.status === Texts.statusApproved);
  const canSubmit = useCan(UiAction.timesheet.submit, { employeeId });
  const canManageWhole = useCan(UiAction.timesheet.finalApprove, { employeeId });

  const changeAttendanceStatus = async (action: TimesheetStatusAction, comment: string, signal: AbortSignal) => {
    await updateTimesheetStatus(
      {
        employeeId,
        year: overview.year,
        month: overview.month,
        action,
        comment,
        timesheetIds: [timesheet.id],
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
      case "submit": {
        const saved = await onSave(signal);
        if (saved.hasErrors) {
          setSubmitBlockedEvaluation(saved);
          setSubmitBlockedOpen(true);
          throw new Error(Texts.workflowSubmitBlockedDescription);
        }
        await changeAttendanceStatus("submit", comment, signal);
        break;
      }
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
          {isDraft && canSubmit && (
            <Button type="button" onClick={() => setActiveWorkflow("submit")}>
              <span className="inline-flex items-center gap-2">
                <Send className="size-4" />
                {Texts.submitForApproval}
              </span>
            </Button>
          )}
          {isSubmitted && canManageWhole && (
            <>
              {allContractPartsApproved && (
                <Button type="button" onClick={() => setActiveWorkflow("finalApprove")}>
                  <span className="inline-flex items-center gap-2">
                    <Check className="size-4" />
                    {Texts.approveTimesheet}
                  </span>
                </Button>
              )}
              <Button type="button" variant="outline" onClick={() => setActiveWorkflow("returnWhole")}>
                <span className="inline-flex items-center gap-2">
                  <RotateCcw className="size-4" />
                  {Texts.returnToDraft}
                </span>
              </Button>
            </>
          )}
          {isApproved && canManageWhole && (
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
          {isDraft && canSubmit && <SaveButton onClick={(_, signal) => handleSaveClick(signal)}>{Texts.saveChanges}</SaveButton>}
        </div>
      </div>
      <TimesheetWorkflowConfirmDialog action={activeWorkflow} periodLabel={periodLabel} onClose={() => setActiveWorkflow(null)} onConfirm={handleWorkflowConfirm} />
      <MessageAlertDialog
        open={submitBlockedOpen}
        title={Texts.workflowSubmitBlockedTitle}
        description={formatSubmitBlockedDescription(submitBlockedEvaluation)}
        onClose={() => setSubmitBlockedOpen(false)}
      />
    </>
  );
};

const formatSubmitBlockedDescription = (evaluation: TimesheetEvaluation | null) => {
  const reasons = [
    ...new Set(
      evaluation?.issues.filter((issue) => issue.type === "error").map((issue) => (issue.code === "ERR-COM-02" || issue.code === "ERR-COM-03" ? "Celková doba za měsíc" : issue.message)) ?? [],
    ),
  ];
  if (reasons.length === 0) {
    return Texts.workflowSubmitBlockedDescription;
  }

  return `${Texts.workflowSubmitBlockedDescription} ${reasons.length === 1 ? "Důvod" : "Důvody"}: ${reasons.join(", ")}.`;
};
