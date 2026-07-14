import { Check, RotateCcw } from "lucide-react";
import { useState } from "react";
import { useRevalidator, useSearchParams } from "react-router";
import { Button } from "@/components/ui/button";
import { Texts } from "@/constants/texts";
import { formatMonthYear } from "@/features/contract/utils/czechMonths";
import { type GetTimesheetOverviewResponse, type TimesheetOverviewItem, type TimesheetStatusAction, updateTimesheetStatus } from "./api";
import { type TimesheetWorkflowAction, TimesheetWorkflowConfirmDialog } from "./TimesheetWorkflowConfirmDialog";

interface TimesheetOverviewRowActionsProps {
  item: TimesheetOverviewItem;
  overview: GetTimesheetOverviewResponse;
}

export const TimesheetOverviewRowActions = ({ item, overview }: TimesheetOverviewRowActionsProps) => {
  const [searchParams] = useSearchParams();
  const revalidator = useRevalidator();
  const [activeWorkflow, setActiveWorkflow] = useState<TimesheetWorkflowAction | null>(null);

  const employeeId = searchParams.get("employeeId") ?? "";
  const periodLabel = formatMonthYear(overview.month, overview.year);
  const timesheetId = item.timesheetId;
  const actions = item.actions;
  const canApprove = Boolean(actions?.approveProject);
  const canReturn = Boolean(actions?.returnProject);

  const changeProjectStatus = async (action: TimesheetStatusAction, comment: string, signal: AbortSignal) => {
    if (!timesheetId) return;

    await updateTimesheetStatus(
      {
        employeeId,
        year: overview.year,
        month: overview.month,
        action,
        comment,
        timesheetIds: [timesheetId],
      },
      signal,
    );
    revalidator.revalidate();
  };

  const handleWorkflowConfirm = async (comment: string, signal: AbortSignal) => {
    if (activeWorkflow === "approveProject") {
      await changeProjectStatus("approve", comment, signal);
    } else if (activeWorkflow === "returnProject") {
      await changeProjectStatus("return", comment, signal);
    }
  };

  if (!canApprove && !canReturn) {
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
