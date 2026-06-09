import type { ReactNode } from "react";
import type { UiActionId, UiContext } from "./uiPermissions";
import { useCan } from "./useCan";

interface CanProps {
  action: UiActionId;
  context?: UiContext;
  children: ReactNode;
}

export const Can = ({ action, context, children }: CanProps) => {
  const allowed = useCan(action, context);
  return allowed ? children : null;
};
