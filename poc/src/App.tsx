import { Outlet } from "react-router";
import { Toaster } from "sonner";
import { AppFooter } from "./common/AppFooter";
import { AppHeader } from "./common/AppHeader";

export const App = () => {
  return (
    <>
      <div className="min-h-screen flex flex-col">
        <AppHeader />
        <main className="flex-1 w-full mx-auto max-w-7xl px-6 py-8">
          <Outlet />
        </main>
        <AppFooter />
      </div>
      <Toaster position="top-center" />
    </>
  );
};
