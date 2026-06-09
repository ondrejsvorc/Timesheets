import { Outlet, useRouteLoaderData } from "react-router";
import { Toaster } from "sonner";
import { RoleViewProvider } from "./auth/RoleViewContext";
import { RoleViewBanner } from "./components/shared/dev/RoleViewBanner";
import { AppFooter } from "./components/shared/layout/AppFooter";
import { AppHeader } from "./components/shared/layout/AppHeader";
import type { RootLoaderData } from "./router";

export const App = () => {
  const rootData = useRouteLoaderData("root") as RootLoaderData | undefined;

  return (
    <RoleViewProvider actualPermissions={rootData?.permissions ?? null}>
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
  );
};
