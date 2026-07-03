import { Check } from "lucide-react";
import { useState } from "react";
import { toast } from "sonner";
import { BusyButton } from "@/components/shared/buttons/BusyButton";
import { DialogCancelButton } from "@/components/shared/buttons/DialogButtons";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { parseApiErrorMessage } from "@/constants/api";
import { Texts } from "@/constants/texts";

export type TimesheetWorkflowAction = "submit" | "finalApprove" | "returnWhole" | "unlock" | "approveProject" | "returnProject";

interface WorkflowActionConfig {
  title: string;
  description: string;
  confirmLabel: string;
}

const replaceTokens = (template: string, periodLabel: string, targetLabel?: string) => template.replace("{period}", periodLabel).replace("{target}", targetLabel ?? "");

const getWorkflowActionConfig = (action: TimesheetWorkflowAction, periodLabel: string, targetLabel?: string): WorkflowActionConfig => {
  switch (action) {
    case "submit":
      return {
        title: Texts.workflowSubmitTitle,
        description: replaceTokens(Texts.workflowSubmitDescription, periodLabel),
        confirmLabel: Texts.submitForApproval,
      };
    case "finalApprove":
      return {
        title: Texts.workflowFinalApproveTitle,
        description: replaceTokens(Texts.workflowFinalApproveDescription, periodLabel),
        confirmLabel: Texts.approveTimesheet,
      };
    case "returnWhole":
      return {
        title: Texts.workflowReturnWholeTitle,
        description: replaceTokens(Texts.workflowReturnWholeDescription, periodLabel),
        confirmLabel: Texts.returnToDraft,
      };
    case "unlock":
      return {
        title: Texts.workflowUnlockTitle,
        description: replaceTokens(Texts.workflowUnlockDescription, periodLabel),
        confirmLabel: Texts.unlockTimesheet,
      };
    case "approveProject":
      return {
        title: Texts.workflowApproveProjectTitle,
        description: replaceTokens(Texts.workflowApproveProjectDescription, periodLabel, targetLabel),
        confirmLabel: Texts.approveProjectPart,
      };
    case "returnProject":
      return {
        title: Texts.workflowReturnProjectTitle,
        description: replaceTokens(Texts.workflowReturnProjectDescription, periodLabel, targetLabel),
        confirmLabel: Texts.returnProjectPart,
      };
  }
};

interface TimesheetWorkflowConfirmDialogProps {
  action: TimesheetWorkflowAction | null;
  periodLabel: string;
  targetLabel?: string;
  onClose: () => void;
  onConfirm: (comment: string, signal: AbortSignal) => Promise<void>;
}

export const TimesheetWorkflowConfirmDialog = ({ action, periodLabel, targetLabel, onClose, onConfirm }: TimesheetWorkflowConfirmDialogProps) => {
  const [comment, setComment] = useState("");
  const open = action !== null;
  const config = action ? getWorkflowActionConfig(action, periodLabel, targetLabel) : null;

  const handleClose = () => {
    setComment("");
    onClose();
  };

  if (!config) {
    return null;
  }

  return (
    <Dialog open={open} onOpenChange={(isOpen) => !isOpen && handleClose()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{config.title}</DialogTitle>
          <DialogDescription>{config.description}</DialogDescription>
        </DialogHeader>
        <div className="space-y-2">
          <Label htmlFor="workflow-comment">{Texts.comment}</Label>
          <Textarea id="workflow-comment" value={comment} onChange={(event) => setComment(event.target.value)} rows={4} placeholder={Texts.writeCommentPlaceholder} />
        </div>
        <DialogFooter>
          <DialogCancelButton onClick={handleClose} />
          <BusyButton
            icon={<Check className="size-4" />}
            onClick={async (_, signal) => {
              await onConfirm(comment, signal);
              handleClose();
            }}
            onSuccess={() => toast.success(Texts.actionSuccessful)}
            onError={(error) => toast.error(parseApiErrorMessage(error, Texts.actionFailed))}
          >
            {config.confirmLabel}
          </BusyButton>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
};
