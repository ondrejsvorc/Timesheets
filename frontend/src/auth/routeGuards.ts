import { redirect } from "react-router";
import { BaseUrl } from "@/constants/api";
import { Routes } from "@/constants/routes";
import type { CurrentUser } from "@/router";
import { getCurrentUserPermissions, type CurrentUserPermissions } from "./api/getCurrentUserPermissions";
import { can, type UiActionId, type UiContext } from "./uiPermissions";

interface AuthContext {
  permissions: CurrentUserPermissions | null;
  currentUser: CurrentUser | null;
}

const loadAuthContext = async (): Promise<AuthContext> => {
  const [permissions, userResponse] = await Promise.all([
    getCurrentUserPermissions().catch(() => null),
    fetch(`${BaseUrl}/auth/currentUser`, { credentials: "include" }),
  ]);

  const currentUser = userResponse.ok ? ((await userResponse.json()) as CurrentUser) : null;
  return { permissions, currentUser };
};

const defaultFallback = (currentUser: CurrentUser | null): string => {
  if (currentUser) {
    return Routes.employee(currentUser.id);
  }

  return Routes.projects();
};

export const denyUnless = async (action: UiActionId, context: UiContext = {}, fallback?: string): Promise<AuthContext> => {
  const auth = await loadAuthContext();

  if (!can(auth.permissions, auth.currentUser?.id, action, context)) {
    throw redirect(fallback ?? defaultFallback(auth.currentUser));
  }

  return auth;
};
