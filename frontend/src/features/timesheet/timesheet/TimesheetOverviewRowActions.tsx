import { Check, RotateCcw } from "lucide-react";
import { useState } from "react";
import { useRevalidator, useSearchParams } from "react-router";
import { UiAction } from "@/auth/uiPermissions";
import { useCan } from "@/auth/useCan";
import { Button } from "@/components/ui/button";
import { Texts } from "@/constants/texts";
import { TimesheetStatusIds } from "@/constants/timesheetStatuses";
import { formatMonthYear } from "@/features/contract/utils/czechMonths";
import type { CombinedTimesheetOverviewItem, GetCombinedTimesheetOverviewResponse } from "./api/getCombinedTimesheetOverview";
import { updateCombinedTimesheetStatus } from "./api/updateCombinedTimesheetStatus";
import { type TimesheetWorkflowAction, TimesheetWorkflowConfirmDialog } from "./TimesheetWorkflowConfirmDialog";

interface TimesheetOverviewRowActionsProps {
  item: CombinedTimesheetOverviewItem;
  overview: GetCombinedTimesheetOverviewResponse;
}

export const TimesheetOverviewRowActions = ({ item, overview }: TimesheetOverviewRowActionsProps) => {
  const [searchParams] = useSearchParams();
  const revalidator = useRevalidator();
  const [activeWorkflow, setActiveWorkflow] = useState<TimesheetWorkflowAction | null>(null);

  const employeeId = searchParams.get("employeeId") ?? "";
  const periodLabel = formatMonthYear(overview.month, overview.year);
  const isSubmitted = overview.status === Texts.statusPendingApproval;

  const timesheetId = item.timesheetId;
  const showActions = item.kind === "project" && Boolean(timesheetId) && isSubmitted;

  const canManagePart = useCan(UiAction.timesheet.approveProject, {
    timesheetContractId: item.contractId ?? undefined,
    timesheetProjectId: item.projectId ?? undefined,
  });
  const canApprove = showActions && canManagePart && item.status === Texts.statusPendingApproval;
  const canReturn = showActions && canManagePart && item.status === Texts.statusPendingApproval;

  const changeProjectStatus = async (statusId: string, comment: string, signal: AbortSignal) => {
    if (!timesheetId) return;

    await updateCombinedTimesheetStatus(
      {
        employeeId,
        year: overview.year,
        month: overview.month,
        statusId,
        comment,
        timesheetIds: [timesheetId],
      },
      signal,
    );
    revalidator.revalidate();
  };

  const handleWorkflowConfirm = async (comment: string, signal: AbortSignal) => {
    if (activeWorkflow === "approveProject") {
      await changeProjectStatus(TimesheetStatusIds.approved, comment, signal);
    } else if (activeWorkflow === "returnProject") {
      await changeProjectStatus(TimesheetStatusIds.draft, comment, signal);
    }
  };

  if (!showActions || (!canApprove && !canReturn)) {
    return Texts.dash;
  }

  return (
    <>
      <div className="flex flex-wrap gap-2">
        {canApprove && (
          <Button type="button" size="sm" onClick={() => setActiveWorkflow("approveProject")}>
            <span className="inline-flex items-center gap-1.5">
              <Check className="size-3.5" />
              {Texts.approveProjectPart}
            </span>
          </Button>
        )}
        {canReturn && (
          <Button type="button" size="sm" variant="outline" onClick={() => setActiveWorkflow("returnProject")}>
            <span className="inline-flex items-center gap-1.5">
              <RotateCcw className="size-3.5" />
              {Texts.returnProjectPart}
            </span>
          </Button>
        )}
      </div>

      <TimesheetWorkflowConfirmDialog action={activeWorkflow} periodLabel={periodLabel} targetLabel={item.label} onClose={() => setActiveWorkflow(null)} onConfirm={handleWorkflowConfirm} />
    </>
  );
};
