import { Lock } from "lucide-react";
import type { ReactNode } from "react";

interface LockableFieldProps {
  locked: boolean;
  children: ReactNode;
}

export const LockableField = ({ locked, children }: LockableFieldProps) => {
  return (
    <div className="flex justify-center w-full min-w-0">
      <div className="relative inline-block">
        {children}
        {locked && (
          <span className="pointer-events-none absolute -top-1 -right-1 z-10">
            <Lock className="size-3 text-primary" />
          </span>
        )}
      </div>
    </div>
  );
};
