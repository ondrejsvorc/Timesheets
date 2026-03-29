import { Button } from "@/components/ui/button";
import { Texts } from "@/constants/texts";
import { ArrowLeft, Lock, Maximize2, Minimize2, Pencil, Plus, Save, Trash2, Unlock } from "lucide-react";
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

interface FullscreenButtonProps {
  onClick: (event: MouseEvent<HTMLButtonElement>) => void | Promise<void>;
  disabled?: boolean;
  isFullscreen?: boolean;
  children?: ReactNode;
}

export const FullscreenIcon = ({ isFullscreen = false }: { isFullscreen?: boolean }) => isFullscreen ? <Minimize2 className="size-4" /> : <Maximize2 className="size-4" />;
export const FullscreenButton = ({ onClick, disabled, isFullscreen = false, children }: FullscreenButtonProps) => (
  <Button type="button" variant="outline" onClick={onClick} disabled={disabled}>
    <span className="inline-flex items-center gap-2">
      <FullscreenIcon isFullscreen={isFullscreen} />
      {children ?? (isFullscreen ? Texts.exitFullscreen : Texts.enterFullscreen)}
    </span>
  </Button>
);

interface BackButtonProps {
  onClick: () => void;
}

export const BackIcon = () => <ArrowLeft className="size-4" />;
export const LockIcon = () => <Lock className="size-4" />;
export const UnlockIcon = () => <Unlock className="size-4" />;

interface ProjectLockToggleButtonProps {
  locked: boolean;
  onClick: (event: MouseEvent<HTMLButtonElement>) => void | Promise<void>;
  disabled?: boolean;
  title?: string;
}

/** Uzamčení sloupce zakázky ve výkazu — stav se odešle při jednotném uložení výkazu. */
export const ProjectLockToggleButton = ({ locked, onClick, disabled, title }: ProjectLockToggleButtonProps) => (
  <Button
    type="button"
    variant="outline"
    size="icon"
    className="h-6 w-6 shrink-0"
    disabled={disabled}
    title={title ?? (locked ? "Odemknout sloupec zakázky" : "Uzamknout sloupec zakázky")}
    aria-label={locked ? "Odemknout sloupec zakázky" : "Uzamknout sloupec zakázky"}
    onClick={onClick}
  >
    {locked ? <Lock className="h-3.5 w-3.5" /> : <Unlock className="h-3.5 w-3.5" />}
  </Button>
);

export const BackButton = ({ onClick }: BackButtonProps) => (
  <Button type="button" variant="outline" size="icon" aria-label="Back" onClick={onClick}>
    <BackIcon />
  </Button>
);
