import type { MouseEvent } from "react";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { DialogCancelButton, DialogConfirmButton } from "./Buttons";
import { Texts } from "./Texts";

interface ConfirmationDialogProps {
  open: boolean;
  onCancel: () => void;
  onConfirm: (event: MouseEvent<HTMLButtonElement>, signal: AbortSignal) => Promise<void>;
}

export const ConfirmationDialog = ({ open, onCancel, onConfirm }: ConfirmationDialogProps) => {
  return (
    <Dialog open={open} onOpenChange={(isOpen) => !isOpen && onCancel()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{Texts.confirmAction}</DialogTitle>
          <DialogDescription>{Texts.confirmActionDescription}</DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <DialogCancelButton onClick={onCancel}></DialogCancelButton>
          <DialogConfirmButton onClick={onConfirm}></DialogConfirmButton>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
};
