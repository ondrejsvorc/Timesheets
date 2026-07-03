import { redirect } from "react-router";
import { onSessionExpired } from "@/constants/api";
import { Routes } from "@/constants/routes";
import { type CurrentUser, getCurrentUser } from "./api";
import { can, UiAction, type UiActionId, type UiContext } from "./uiPermissions";

let cachedUser: CurrentUser | null = null;
let userRequest: Promise<CurrentUser | null> | null = null;

export const loadCurrentUser = async (): Promise<CurrentUser | null> => {
  if (cachedUser) {
    return cachedUser;
  }

  if (userRequest) {
    return userRequest;
  }

  userRequest = getCurrentUser()
    .then((user) => {
      cachedUser = user;
      return user;
    })
    .finally(() => {
      userRequest = null;
    });

  return userRequest;
};

export const resetCurrentUser = () => {
  cachedUser = null;
  userRequest = null;
};

onSessionExpired(resetCurrentUser);

export const resolveHomePath = (user: CurrentUser): string => {
  if (can(user.permissions, user.id, UiAction.nav.projects)) {
    return Routes.projects();
  }

  return Routes.employee(user.id);
};

const loginRedirectPath = (request?: Request): string => {
  const returnTo = request ? `${new URL(request.url).pathname}${new URL(request.url).search}` : "/";
  return `/redirecting?returnTo=${encodeURIComponent(returnTo)}`;
};

const defaultFallback = (user: CurrentUser | null, request?: Request): string => {
  if (user) {
    return Routes.employee(user.id);
  }

  return loginRedirectPath(request);
};

export const denyUnless = async (action: UiActionId, context: UiContext = {}, request?: Request): Promise<CurrentUser> => {
  const user = await loadCurrentUser();

  if (!user || !can(user.permissions, user.id, action, context)) {
    const target = defaultFallback(user, request);
    const pathname = request ? new URL(request.url).pathname : null;
    if (pathname && pathname === target) {
      throw redirect(loginRedirectPath(request));
    }
    throw redirect(target);
  }

  return user;
};
