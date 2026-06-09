import { Check, RotateCcw, Send } from "lucide-react";
import { useState } from "react";
import { useRevalidator, useRouteLoaderData, useSearchParams } from "react-router";
import { useEffectivePermissions } from "@/auth/RoleViewContext";
import { canManageWholeTimesheet, canSubmitTimesheet } from "@/auth/timesheetPermissions";
import { FullscreenButton, SaveButton, UnlockIcon } from "@/components/shared/buttons/ActionButtons";
import { MessageAlertDialog } from "@/components/shared/dialogs/MessageAlertDialog";
import { Button } from "@/components/ui/button";
import { Texts } from "@/constants/texts";
import { TimesheetStatusIds } from "@/constants/timesheetStatuses";
import { formatMonthYear } from "@/features/contract/utils/czechMonths";
import type { RootLoaderData } from "@/router";
import type { Timesheet } from "../Timesheet";
import { TimesheetValidations } from "../TimesheetValidations";
import type { GetCombinedTimesheetOverviewResponse } from "./api/getCombinedTimesheetOverview";
import { updateCombinedTimesheetStatus } from "./api/updateCombinedTimesheetStatus";
import { type TimesheetWorkflowAction, TimesheetWorkflowConfirmDialog } from "./TimesheetWorkflowConfirmDialog";
import { useTimesheetWorkflowRefresh } from "./TimesheetWorkflowRefreshContext";
import { areAllProjectsApproved } from "./timesheetWorkflowUtils";

interface TimesheetWorkflowToolbarProps {
  timesheet: Timesheet;
  overview: GetCombinedTimesheetOverviewResponse;
  isFullscreen: boolean;
  onToggleFullscreen: () => void;
  onSave: (signal: AbortSignal) => Promise<void>;
  onClearAttendanceFields: () => void;
}

export const TimesheetWorkflowToolbar = ({
  timesheet,
  overview,
  isFullscreen,
  onToggleFullscreen,
  onSave,
  onClearAttendanceFields,
}: TimesheetWorkflowToolbarProps) => {
  const [searchParams] = useSearchParams();
  const rootData = useRouteLoaderData("root") as RootLoaderData | undefined;
  const currentUserId = rootData?.currentUser?.id;
  const { permissions } = useEffectivePermissions();
  const revalidator = useRevalidator();
  const onWorkflowSuccess = useTimesheetWorkflowRefresh();
  const [activeWorkflow, setActiveWorkflow] = useState<TimesheetWorkflowAction | null>(null);
  const [submitBlockedOpen, setSubmitBlockedOpen] = useState(false);

  const employeeId = searchParams.get("employeeId") ?? "";
  const periodLabel = formatMonthYear(overview.month, overview.year);
  const isDraft = overview.status === Texts.statusInProgress;
  const isSubmitted = overview.status === Texts.statusPendingApproval;
  const isApproved = overview.status === Texts.statusApproved;
  const allProjectsApproved = areAllProjectsApproved(overview);
  const canSubmit = canSubmitTimesheet(currentUserId, employeeId);
  const canManageWhole = canManageWholeTimesheet(permissions);

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
    onWorkflowSuccess();
  };

  const handleSubmitClick = () => {
    const validations = TimesheetValidations.validateTimesheet(timesheet);
    if (TimesheetValidations.hasErrors(validations)) {
      setSubmitBlockedOpen(true);
      return;
    }
    setActiveWorkflow("submit");
  };

  const handleWorkflowConfirm = async (comment: string, signal: AbortSignal) => {
    switch (activeWorkflow) {
      case "submit":
        await onSave(signal);
        await changeAttendanceStatus(TimesheetStatusIds.submitted, comment, signal);
        break;
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
          {isDraft && canSubmit && <SaveButton onClick={(_, signal) => onSave(signal)}>{Texts.saveChanges}</SaveButton>}
        </div>
      </div>

      <TimesheetWorkflowConfirmDialog
        action={activeWorkflow}
        periodLabel={periodLabel}
        onClose={() => setActiveWorkflow(null)}
        onConfirm={handleWorkflowConfirm}
      />
      <MessageAlertDialog
        open={submitBlockedOpen}
        title={Texts.workflowSubmitBlockedTitle}
        description={Texts.workflowSubmitBlockedDescription}
        onClose={() => setSubmitBlockedOpen(false)}
      />
    </>
  );
};
