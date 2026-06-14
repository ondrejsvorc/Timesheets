import { Trash2 } from "lucide-react";
import type { MouseEvent, ReactNode } from "react";
import { toast } from "sonner";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { parseApiErrorMessage } from "@/constants/api";
import { Texts } from "@/constants/texts";
import { cn } from "@/utils/cn";
import { BusyButton } from "../buttons/BusyButton";
import { DialogCancelButton } from "../buttons/DialogButtons";

interface ConsequenceDialogProps {
  open: boolean;
  title: string;
  description: string;
  consequences: string[];
  confirmLabel: string;
  confirmDisabled?: boolean;
  loading?: boolean;
  loadingContent?: ReactNode;
  onCancel: () => void;
  onConfirm: (event: MouseEvent<HTMLButtonElement>, signal: AbortSignal) => Promise<void>;
}

export const ConsequenceDialog = ({ open, title, description, consequences, confirmLabel, confirmDisabled = false, loading = false, loadingContent, onCancel, onConfirm }: ConsequenceDialogProps) => {
  return (
    <Dialog open={open} onOpenChange={(isOpen) => !isOpen && onCancel()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
          <DialogDescription>{description}</DialogDescription>
        </DialogHeader>

        {loading ? (
          loadingContent
        ) : (
          <ul className={cn("list-disc space-y-1.5 pl-5 text-sm text-muted-foreground")}>
            {consequences.map((consequence) => (
              <li key={consequence}>{consequence}</li>
            ))}
          </ul>
        )}

        <DialogFooter>
          <DialogCancelButton onClick={onCancel} />
          <BusyButton
            variant="destructive"
            disabled={confirmDisabled || loading}
            icon={<Trash2 className="size-4" />}
            onClick={onConfirm}
            onSuccess={() => toast.success(Texts.actionSuccessful)}
            onError={(error) => toast.error(parseApiErrorMessage(error, Texts.actionFailed))}
          >
            {confirmLabel}
          </BusyButton>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
};
