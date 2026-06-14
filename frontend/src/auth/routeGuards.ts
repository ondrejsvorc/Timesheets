import { redirect } from "react-router";
import { BaseUrl, goToLogin } from "@/constants/api";
import { Routes } from "@/constants/routes";
import type { CurrentUser } from "@/router";
import { type CurrentUserPermissions, getCurrentUserPermissions } from "./api/getCurrentUserPermissions";
import { can, UiAction, type UiActionId, type UiContext } from "./uiPermissions";

export interface AuthContext {
  permissions: CurrentUserPermissions | null;
  currentUser: CurrentUser | null;
}

let cachedAuth: AuthContext | null = null;
let authRequest: Promise<AuthContext> | null = null;

const fetchAuthContext = async (): Promise<AuthContext> => {
  const [permissions, userResponse] = await Promise.all([
    getCurrentUserPermissions().catch(() => null),
    fetch(`${BaseUrl}/auth/currentUser`, { credentials: "include" }),
  ]);

  if (userResponse.status === 401) {
    resetAuthContext();
    goToLogin();
    return new Promise(() => {});
  }

  const currentUser = userResponse.ok ? ((await userResponse.json()) as CurrentUser) : null;
  return { permissions, currentUser };
};

export const loadAuthContext = async (): Promise<AuthContext> => {
  if (cachedAuth) {
    return cachedAuth;
  }

  if (authRequest) {
    return authRequest;
  }

  authRequest = fetchAuthContext()
    .then((auth) => {
      cachedAuth = auth;
      return auth;
    })
    .finally(() => {
      authRequest = null;
    });

  return authRequest;
};

export const resetAuthContext = () => {
  cachedAuth = null;
  authRequest = null;
};

export const resolveHomePath = (auth: AuthContext): string => {
  if (can(auth.permissions, auth.currentUser?.id, UiAction.nav.projects)) {
    return Routes.projects();
  }

  if (auth.currentUser) {
    return Routes.employee(auth.currentUser.id);
  }

  return `/redirecting?returnTo=${encodeURIComponent("/")}`;
};

const loginRedirectPath = (request?: Request): string => {
  const returnTo = request ? `${new URL(request.url).pathname}${new URL(request.url).search}` : "/";
  return `/redirecting?returnTo=${encodeURIComponent(returnTo)}`;
};

const defaultFallback = (auth: AuthContext, request?: Request): string => {
  if (auth.currentUser) {
    return Routes.employee(auth.currentUser.id);
  }

  return loginRedirectPath(request);
};

export const denyUnless = async (action: UiActionId, context: UiContext = {}, request?: Request): Promise<AuthContext> => {
  const auth = await loadAuthContext();

  if (!can(auth.permissions, auth.currentUser?.id, action, context)) {
    const target = defaultFallback(auth, request);
    const pathname = request ? new URL(request.url).pathname : null;
    if (pathname && pathname === target) {
      throw redirect(loginRedirectPath(request));
    }
    throw redirect(target);
  }

  return auth;
};
