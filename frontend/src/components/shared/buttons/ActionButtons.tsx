import { Button } from "@/components/ui/button";
import { Texts } from "@/constants/texts";
import { ArrowLeft, Pencil, Plus, Save, Trash2 } from "lucide-react";
import type { MouseEvent, ReactNode } from "react";
import { toast } from "sonner";
import { BusyButton } from "./BusyButton";

export const ActionButtons = ({ children }: { children: ReactNode }) => (
  <div className="flex items-center gap-2">{children}</div>
);

interface AddButtonProps {
  onClick: (event: MouseEvent<HTMLButtonElement>) => void | Promise<void>;
  disabled?: boolean;
  children?: ReactNode;
}

export const AddIcon = () => <Plus className="size-4" />;
export const AddButton = ({ onClick, disabled, children }: AddButtonProps) => (
  <Button type="button" onClick={onClick} disabled={disabled}>
    <span className="inline-flex items-center gap-2">
      <AddIcon />
      {children}
    </span>
  </Button>
);

interface EditButtonProps {
  onClick: (event: MouseEvent<HTMLButtonElement>) => void | Promise<void>;
  disabled?: boolean;
  children?: ReactNode;
}

export const EditIcon = () => <Pencil className="size-4" />;
export const EditButton = ({ onClick, disabled, children }: EditButtonProps) => (
  <Button type="button" variant="outline" onClick={onClick} disabled={disabled}>
    <span className="inline-flex items-center gap-2">
      <EditIcon />
      {children}
    </span>
  </Button>
);

interface DeleteButtonProps {
  onClick: (event: MouseEvent<HTMLButtonElement>) => void | Promise<void>;
  disabled?: boolean;
  children?: ReactNode;
}

export const DeleteIcon = () => <Trash2 className="size-4" />;
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
  children?: ReactNode;
}

export const SaveIcon = () => <Save className="size-4" />;
export const SaveButton = ({ onClick, disabled, children }: SaveButtonProps) => (
  <BusyButton
    onClick={onClick}
    disabled={disabled}
    icon={<Save className="size-4" />}
    type="submit"
    onSuccess={() => toast.success(Texts.actionSuccessful)}
    onError={() => toast.error(Texts.actionFailed)}
  >
    {children ?? null}
  </BusyButton>
);

interface BackButtonProps {
  onClick: () => void;
}

export const BackIcon = () => <ArrowLeft className="size-4" />;
export const BackButton = ({ onClick }: BackButtonProps) => (
  <Button type="button" variant="outline" size="icon" aria-label="Back" onClick={onClick}>
    <BackIcon />
  </Button>
);
