import { Loader2 } from "lucide-react";
import type { ComponentProps, MouseEvent, ReactNode } from "react";
import { useRef, useState } from "react";
import { Button } from "@/components/ui/button";

interface BusyButtonProps {
  onClick: (event: MouseEvent<HTMLButtonElement>, signal: AbortSignal) => Promise<void>;
  disabled?: boolean;
  icon: ReactNode;
  children: ReactNode;
  type?: "button" | "submit";
  variant?: ComponentProps<typeof Button>["variant"];
  onSuccess?: () => void;
  onError?: (error: unknown) => void;
}

export const BusyButton = ({ onClick, disabled = false, icon, children, type = "button", variant, onSuccess, onError }: BusyButtonProps) => {
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
    <Button type={type} variant={variant} onClick={handleClick} disabled={disabled || isBusy}>
      <span className="inline-flex items-center gap-2">
        {isBusy ? <Loader2 className="size-4 animate-spin opacity-60 [animation-duration:0.5s]" /> : icon}
        {children}
      </span>
    </Button>
  );
};
