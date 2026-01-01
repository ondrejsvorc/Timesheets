import { ArrowLeft, Check, Loader2, Pencil, Plus, Save, Trash2 } from "lucide-react";
import type { MouseEvent, ReactNode } from "react";
import { useRef, useState } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Texts } from "./Texts";

interface BusyButtonProps {
  onClick: (event: MouseEvent<HTMLButtonElement>, signal: AbortSignal) => Promise<void>;
  disabled?: boolean;
  icon: ReactNode;
  children: ReactNode;
  type?: "button" | "submit";
  onSuccess?: () => void;
  onError?: (error: unknown) => void;
}

const BusyButton = ({ onClick, disabled = false, icon, children, type = "button", onSuccess, onError }: BusyButtonProps) => {
  const [isBusy, setIsBusy] = useState(false);
  const abortRef = useRef<AbortController | null>(null);

  const handleClick = async (event: MouseEvent<HTMLButtonElement>) => {
    if (isBusy) {
      return;
    }

    const controller = new AbortController();
    abortRef.current = controller;

    setIsBusy(true);
    try {
      await onClick(event, controller.signal);
      if (!controller.signal.aborted) {
        onSuccess?.();
      }
    } catch (error) {
      if (error instanceof DOMException && error.name === "AbortError") {
        return;
      }
      onError?.(error);
    } finally {
      setIsBusy(false);
      abortRef.current = null;
    }
  };

  return (
    <Button type={type} onClick={handleClick} disabled={disabled || isBusy}>
      <span className="inline-flex items-center gap-2">
        {isBusy ? <Loader2 className="size-4 animate-spin opacity-60 [animation-duration:0.5s]" /> : icon}
        {children}
      </span>
    </Button>
  );
};

interface SaveButtonProps {
  onClick: (event: MouseEvent<HTMLButtonElement>, signal: AbortSignal) => Promise<void>;
  disabled?: boolean;
  children: ReactNode;
}

export const SaveButton = ({ onClick, disabled, children }: SaveButtonProps) => (
  <BusyButton
    onClick={onClick}
    disabled={disabled}
    icon={<Save className="size-4" />}
    type="submit"
    onSuccess={() => toast.success(Texts.actionSuccessful)}
    onError={() => toast.error(Texts.actionFailed)}
  >
    {children}
  </BusyButton>
);

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
    onError={() => toast.error(Texts.actionFailed)}
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

interface AddButtonProps {
  onClick: (event: MouseEvent<HTMLButtonElement>) => void | Promise<void>;
  disabled?: boolean;
  children: ReactNode;
}

export const AddButton = ({ onClick, disabled, children }: AddButtonProps) => (
  <Button type="button" onClick={onClick} disabled={disabled}>
    <span className="inline-flex items-center gap-2">
      <Plus className="size-4" />
      {children}
    </span>
  </Button>
);

interface EditButtonProps {
  onClick: (event: MouseEvent<HTMLButtonElement>) => void | Promise<void>;
  disabled?: boolean;
  children: ReactNode;
}

export const EditButton = ({ onClick, disabled, children }: EditButtonProps) => (
  <Button type="button" variant="outline" onClick={onClick} disabled={disabled}>
    <span className="inline-flex items-center gap-2">
      <Pencil className="size-4" />
      {children}
    </span>
  </Button>
);

interface DeleteButtonProps {
  onClick: (event: MouseEvent<HTMLButtonElement>) => void | Promise<void>;
  disabled?: boolean;
  children: ReactNode;
}

export const DeleteButton = ({ onClick, disabled, children }: DeleteButtonProps) => (
  <Button type="button" variant="destructive" onClick={onClick} disabled={disabled}>
    <span className="inline-flex items-center gap-2">
      <Trash2 className="size-4" />
      {children}
    </span>
  </Button>
);

interface BackButtonProps {
  onClick: () => void;
}

export const BackButton = ({ onClick }: BackButtonProps) => (
  <Button type="button" variant="outline" size="icon" aria-label="Back" onClick={onClick}>
    <ArrowLeft className="size-4" />
  </Button>
);
