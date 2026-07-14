import { useCurrentUser } from "./CurrentUserContext";
import { useEffectivePermissions } from "./RoleViewContext";
import { can, type UiActionId, type UiContext } from "./uiPermissions";

export const useCan = (action: UiActionId, context: UiContext = {}): boolean => {
  const { permissions } = useEffectivePermissions();
  const currentUser = useCurrentUser();

  return can(permissions, currentUser.id, action, context);
};
