import type { ReactNode } from "react";
import { useCan } from "./useCan";
import type { UiActionId, UiContext } from "./uiPermissions";

interface CanProps {
  action: UiActionId;
  context?: UiContext;
  children: ReactNode;
}

export const Can = ({ action, context, children }: CanProps) => {
  const allowed = useCan(action, context);
  return allowed ? children : null;
};
