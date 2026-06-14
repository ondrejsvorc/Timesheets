import type { ReactNode } from "react";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";

interface FormDialogProps {
  open: boolean;
  title: string;
  onClose: () => void;
  onOpenChange?: (open: boolean) => void;
  children: ReactNode;
}

export const FormDialog = ({ open, title, onClose, onOpenChange, children }: FormDialogProps) => (
  <Dialog
    open={open}
    onOpenChange={(isOpen) => {
      if (onOpenChange) {
        onOpenChange(isOpen);
        return;
      }
      if (!isOpen) {
        onClose();
      }
    }}
  >
    <DialogContent>
      <DialogHeader>
        <DialogTitle>{title}</DialogTitle>
      </DialogHeader>
      {children}
    </DialogContent>
  </Dialog>
);
