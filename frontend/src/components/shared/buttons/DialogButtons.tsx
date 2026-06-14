import { Check } from "lucide-react";
import type { MouseEvent } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { parseApiErrorMessage } from "@/constants/api";
import { Texts } from "@/constants/texts";
import { BusyButton } from "./BusyButton";

interface DialogConfirmButtonProps {
  onClick: (event: MouseEvent<HTMLButtonElement>, signal: AbortSignal) => Promise<void>;
  disabled?: boolean;
}

export const DialogConfirmButton = ({ onClick, disabled }: DialogConfirmButtonProps) => (
  <BusyButton
    onClick={onClick}
    disabled={disabled}
    icon={<Check className="size-4" />}
    type="submit"
    onSuccess={() => toast.success(Texts.actionSuccessful)}
    onError={(error) => toast.error(parseApiErrorMessage(error, Texts.actionFailed))}
  >
    {Texts.confirm}
  </BusyButton>
);

interface DialogCancelButtonProps {
  onClick: (event: MouseEvent<HTMLButtonElement>) => void | Promise<void>;
}

export const DialogCancelButton = ({ onClick }: DialogCancelButtonProps) => (
  <Button type="button" variant="ghost" onClick={onClick}>
    {Texts.cancel}
  </Button>
);
