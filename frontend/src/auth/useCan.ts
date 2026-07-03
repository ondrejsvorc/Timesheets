import { useRouteLoaderData } from "react-router";
import type { CurrentUser } from "./api";
import { useEffectivePermissions } from "./RoleViewContext";
import { can, type UiActionId, type UiContext } from "./uiPermissions";

export const useCan = (action: UiActionId, context: UiContext = {}): boolean => {
  const { permissions } = useEffectivePermissions();
  const currentUser = useRouteLoaderData("root") as CurrentUser | undefined;

  return can(permissions, currentUser?.id, action, context);
};
