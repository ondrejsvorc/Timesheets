import { Check } from "lucide-react";
import { useEffect, useState } from "react";
import { toast } from "sonner";
import { BusyButton } from "@/components/shared/buttons/BusyButton";
import { DialogCancelButton } from "@/components/shared/buttons/DialogButtons";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Texts } from "@/constants/texts";

export type TimesheetWorkflowAction = "submit" | "approve" | "return" | "unlock";

interface WorkflowActionConfig {
  title: string;
  description: string;
  confirmLabel: string;
}

const getWorkflowActionConfig = (action: TimesheetWorkflowAction, periodLabel: string): WorkflowActionConfig => {
  const replacePeriod = (template: string) => template.replace("{period}", periodLabel);

  switch (action) {
    case "submit":
      return {
        title: Texts.workflowSubmitTitle,
        description: replacePeriod(Texts.workflowSubmitDescription),
        confirmLabel: Texts.submitForApproval,
      };
    case "approve":
      return {
        title: Texts.workflowApproveTitle,
        description: replacePeriod(Texts.workflowApproveDescription),
        confirmLabel: Texts.approveTimesheet,
      };
    case "return":
      return {
        title: Texts.workflowReturnTitle,
        description: replacePeriod(Texts.workflowReturnDescription),
        confirmLabel: Texts.returnToDraft,
      };
    case "unlock":
      return {
        title: Texts.workflowUnlockTitle,
        description: replacePeriod(Texts.workflowUnlockDescription),
        confirmLabel: Texts.unlockTimesheet,
      };
  }
};

interface TimesheetWorkflowConfirmDialogProps {
  action: TimesheetWorkflowAction | null;
  periodLabel: string;
  onClose: () => void;
  onConfirm: (comment: string, signal: AbortSignal) => Promise<void>;
}

export const TimesheetWorkflowConfirmDialog = ({ action, periodLabel, onClose, onConfirm }: TimesheetWorkflowConfirmDialogProps) => {
  const [comment, setComment] = useState("");
  const open = action !== null;
  const config = action ? getWorkflowActionConfig(action, periodLabel) : null;

  useEffect(() => {
    if (!open) {
      setComment("");
    }
  }, [open]);

  if (!config) {
    return null;
  }

  return (
    <Dialog open={open} onOpenChange={(isOpen) => !isOpen && onClose()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{config.title}</DialogTitle>
          <DialogDescription>{config.description}</DialogDescription>
        </DialogHeader>

        <div className="space-y-2">
          <Label htmlFor="workflow-comment">{Texts.comment}</Label>
          <Textarea
            id="workflow-comment"
            value={comment}
            onChange={(event) => setComment(event.target.value)}
            rows={4}
            placeholder={Texts.writeCommentPlaceholder}
          />
        </div>

        <DialogFooter>
          <DialogCancelButton onClick={onClose} />
          <BusyButton
            icon={<Check className="size-4" />}
            onClick={async (_, signal) => {
              await onConfirm(comment, signal);
              onClose();
            }}
            onSuccess={() => toast.success(Texts.actionSuccessful)}
            onError={() => toast.error(Texts.actionFailed)}
          >
            {config.confirmLabel}
          </BusyButton>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
};
