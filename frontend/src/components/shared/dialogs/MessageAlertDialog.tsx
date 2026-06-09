import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { Texts } from "@/constants/texts";

interface MessageAlertDialogProps {
  open: boolean;
  title: string;
  description: string;
  onClose: () => void;
  closeLabel?: string;
}

export const MessageAlertDialog = ({ open, title, description, onClose, closeLabel = Texts.close }: MessageAlertDialogProps) => (
  <AlertDialog open={open} onOpenChange={(isOpen) => !isOpen && onClose()}>
    <AlertDialogContent>
      <AlertDialogHeader>
        <AlertDialogTitle>{title}</AlertDialogTitle>
        <AlertDialogDescription>{description}</AlertDialogDescription>
      </AlertDialogHeader>
      <AlertDialogFooter>
        <AlertDialogAction onClick={onClose}>{closeLabel}</AlertDialogAction>
      </AlertDialogFooter>
    </AlertDialogContent>
  </AlertDialog>
);
