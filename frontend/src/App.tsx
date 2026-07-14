import { Outlet, useLoaderData } from "react-router";
import { Toaster } from "sonner";
import type { CurrentUser } from "./auth/api";
import { CurrentUserContext } from "./auth/CurrentUserContext";
import { RoleViewProvider } from "./auth/RoleViewContext";
import { RoleViewBanner } from "./components/shared/dev/RoleViewBanner";
import { AppFooter } from "./components/shared/layout/AppFooter";
import { AppHeader } from "./components/shared/layout/AppHeader";

export const App = () => {
  const currentUser = useLoaderData() as CurrentUser;

  return (
    <CurrentUserContext.Provider value={currentUser}>
      <RoleViewProvider>
        <div className="min-h-screen flex flex-col bg-background">
          <AppHeader />
          <RoleViewBanner />
          <main className="flex-1 w-full mx-auto max-w-7xl px-6 py-10">
            <Outlet />
          </main>
          <AppFooter />
        </div>
        <Toaster position="bottom-center" />
      </RoleViewProvider>
    </CurrentUserContext.Provider>
  );
};
