import { createContext, useContext } from "react";
import type { CurrentUser } from "./api";

export const CurrentUserContext = createContext<CurrentUser | null>(null);

export const useCurrentUser = (): CurrentUser => {
  const user = useContext(CurrentUserContext);
  if (!user) {
    throw new Error("useCurrentUser must be used inside CurrentUserContext.Provider");
  }
  return user;
};
