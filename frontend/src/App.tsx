import { useEffect } from "react";
import { Outlet } from "react-router";
import { Toaster } from "sonner";
import { AppFooter } from "./components/shared/layout/AppFooter";
import { AppHeader } from "./components/shared/layout/AppHeader";
import { BaseUrl } from "./constants/api";

export const App = () => {
  useEffect(() => {
    const run = async () => {
      try {
        const response = await fetch(`${BaseUrl}/auth/currentUser`, { credentials: "include" });
        const text = await response.text();

        if (!response.ok) {
          console.log("[AUTH DEBUG] /auth/currentUser failed", {
            status: response.status,
            statusText: response.statusText,
            body: text,
          });
          return;
        }

        try {
          console.log("[AUTH DEBUG] /auth/currentUser", JSON.parse(text));
        } catch {
          console.log("[AUTH DEBUG] /auth/currentUser (non-JSON)", text);
        }
      } catch (error) {
        console.log("[AUTH DEBUG] /auth/currentUser error", error);
      }
    };

    void run();
  }, []);

  return (
    <>
      <div className="min-h-screen flex flex-col bg-background">
        <AppHeader />
        <main className="flex-1 w-full mx-auto max-w-7xl px-6 py-10">
          <Outlet />
        </main>
        <AppFooter />
      </div>
      <Toaster position="bottom-center" />
    </>
  );
};
