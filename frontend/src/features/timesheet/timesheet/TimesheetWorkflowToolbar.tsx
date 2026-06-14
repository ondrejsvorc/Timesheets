import { Check, RotateCcw, Send } from "lucide-react";
import { useState } from "react";
import { useRevalidator, useSearchParams } from "react-router";
import { toast } from "sonner";
import { UiAction } from "@/auth/uiPermissions";
import { useCan } from "@/auth/useCan";
import { FullscreenButton, SaveButton, UnlockIcon } from "@/components/shared/buttons/ActionButtons";
import { MessageAlertDialog } from "@/components/shared/dialogs/MessageAlertDialog";
import { Button } from "@/components/ui/button";
import { Texts } from "@/constants/texts";
import { TimesheetStatusIds } from "@/constants/timesheetStatuses";
import { formatMonthYear } from "@/features/contract/utils/czechMonths";
import type { Timesheet } from "../Timesheet";
import { TimesheetValidations } from "../TimesheetValidations";
import type { GetCombinedTimesheetOverviewResponse } from "./api/getCombinedTimesheetOverview";
import { updateCombinedTimesheetStatus } from "./api/updateCombinedTimesheetStatus";
import { type TimesheetWorkflowAction, TimesheetWorkflowConfirmDialog } from "./TimesheetWorkflowConfirmDialog";

interface TimesheetWorkflowToolbarProps {
  timesheet: Timesheet;
  overview: GetCombinedTimesheetOverviewResponse;
  isFullscreen: boolean;
  onToggleFullscreen: () => void;
  onSave: (signal: AbortSignal) => Promise<void>;
  onClearAttendanceFields: () => void;
}

export const TimesheetWorkflowToolbar = ({ timesheet, overview, isFullscreen, onToggleFullscreen, onSave, onClearAttendanceFields }: TimesheetWorkflowToolbarProps) => {
  const [searchParams] = useSearchParams();
  const revalidator = useRevalidator();
  const [activeWorkflow, setActiveWorkflow] = useState<TimesheetWorkflowAction | null>(null);
  const [submitBlockedOpen, setSubmitBlockedOpen] = useState(false);

  const employeeId = searchParams.get("employeeId") ?? "";
  const periodLabel = formatMonthYear(overview.month, overview.year);
  const isDraft = overview.status === Texts.statusInProgress;
  const isSubmitted = overview.status === Texts.statusPendingApproval;
  const isApproved = overview.status === Texts.statusApproved;
  const projectItems = overview.items.filter((item) => item.kind === "project");
  const allProjectsApproved = projectItems.length === 0 || projectItems.every((item) => item.status === Texts.statusApproved);
  const canSubmit = useCan(UiAction.timesheet.submit, { employeeId });
  const canManageWhole = useCan(UiAction.timesheet.finalApprove);

  const changeAttendanceStatus = async (statusId: string, comment: string, signal: AbortSignal) => {
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
  };

  const hasValidationErrors = (): boolean => TimesheetValidations.hasErrors(TimesheetValidations.validateForSubmit(timesheet));

  const handleSubmitClick = () => {
    if (hasValidationErrors()) {
      setSubmitBlockedOpen(true);
      return;
    }
    setActiveWorkflow("submit");
  };

  const handleSaveClick = async (signal: AbortSignal) => {
    if (hasValidationErrors()) {
      toast.error(Texts.workflowSaveBlockedTitle, { description: Texts.workflowSaveBlockedDescription });
      return;
    }
    await onSave(signal);
  };

  const handleWorkflowConfirm = async (comment: string, signal: AbortSignal) => {
    switch (activeWorkflow) {
      case "submit": {
        await onSave(signal);
        const validations = TimesheetValidations.validateForSubmit(timesheet);
        if (TimesheetValidations.hasErrors(validations)) {
          setSubmitBlockedOpen(true);
          throw new Error(Texts.workflowSubmitBlockedDescription);
        }
        await changeAttendanceStatus(TimesheetStatusIds.submitted, comment, signal);
        break;
      }
      case "finalApprove":
        await changeAttendanceStatus(TimesheetStatusIds.approved, comment, signal);
        break;
      case "returnWhole":
        await changeAttendanceStatus(TimesheetStatusIds.draft, comment, signal);
        break;
      case "unlock":
        await changeAttendanceStatus(TimesheetStatusIds.draft, comment, signal);
        break;
      default:
        break;
    }
  };

  return (
    <>
      <div className="mb-6 flex flex-wrap items-center justify-between gap-3">
        <div className="flex flex-wrap items-center gap-3">
          {isDraft && canSubmit && (
            <>
              <Button type="button" onClick={handleSubmitClick}>
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
          {isSubmitted && canManageWhole && (
            <>
              {allProjectsApproved && (
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
      <MessageAlertDialog open={submitBlockedOpen} title={Texts.workflowSubmitBlockedTitle} description={Texts.workflowSubmitBlockedDescription} onClose={() => setSubmitBlockedOpen(false)} />
    </>
  );
};
