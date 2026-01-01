import { ArrowLeft, Pencil, Plus, Save, Trash2 } from "lucide-react";
import type { MouseEvent, ReactNode } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Texts } from "@/constants/texts";
import { BusyButton } from "./BusyButton";

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

interface BackButtonProps {
  onClick: () => void;
}

export const BackButton = ({ onClick }: BackButtonProps) => (
  <Button type="button" variant="outline" size="icon" aria-label="Back" onClick={onClick}>
    <ArrowLeft className="size-4" />
  </Button>
);
