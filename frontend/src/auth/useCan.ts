import { useRouteLoaderData } from "react-router";
import type { RootLoaderData } from "@/router";
import { useEffectivePermissions } from "./RoleViewContext";
import { can, type UiActionId, type UiContext } from "./uiPermissions";

export const useCan = (action: UiActionId, context: UiContext = {}): boolean => {
  const { permissions } = useEffectivePermissions();
  const rootData = useRouteLoaderData("root") as RootLoaderData | undefined;
  const currentUserId = rootData?.currentUser?.id;

  return can(permissions, currentUserId, action, context);
};
