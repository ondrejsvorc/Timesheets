import { User } from "lucide-react";
import { Link } from "react-router";
import { Can } from "@/auth/Can";
import { useCurrentUser } from "@/auth/CurrentUserContext";
import { UiAction } from "@/auth/uiPermissions";
import { useCan } from "@/auth/useCan";
import { RoleViewSwitcher } from "@/components/shared/dev/RoleViewSwitcher";
import { Button } from "@/components/ui/button";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuLabel, DropdownMenuSeparator, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import { BaseUrl } from "@/constants/api";
import { Routes } from "@/constants/routes";
import { Texts } from "../../../constants/texts";

export const AppHeader = () => {
  const currentUser = useCurrentUser();
  const canNavProjects = useCan(UiAction.nav.projects);
  const canNavEmployees = useCan(UiAction.nav.employees);
  const canNavMyTimesheets = useCan(UiAction.nav.myTimesheets);
  const homeRoute = canNavProjects ? Routes.projects() : Routes.employee(currentUser.id);

  const handleLogout = () => {
    window.location.assign(`${BaseUrl}/auth/logout`);
  };

  return (
    <header className="w-full border-b border-border/50 bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60 sticky top-0 z-50">
      <div className="mx-auto max-w-7xl flex h-16 items-center justify-between px-6">
        {/* Brand */}
        <Link to={homeRoute} className="text-xl font-semibold tracking-tight text-primary select-none hover:text-primary/90 transition-colors">
          {Texts.applicationName}
        </Link>

        {/* Navigation */}
        <nav className="flex items-center gap-1">
          {canNavProjects && (
            <Link to="/projects" className="px-4 py-2 text-sm font-medium text-foreground/70 hover:text-foreground hover:bg-accent rounded-md transition-all">
              {Texts.projects}
            </Link>
          )}
          {canNavEmployees && (
            <Link to="/employees" className="px-4 py-2 text-sm font-medium text-foreground/70 hover:text-foreground hover:bg-accent rounded-md transition-all">
              {Texts.employees}
            </Link>
          )}
          {canNavMyTimesheets && (
            <Link to={Routes.employee(currentUser.id)} className="px-4 py-2 text-sm font-medium text-foreground/70 hover:text-foreground hover:bg-accent rounded-md transition-all">
              {Texts.myTimesheets}
            </Link>
          )}
          <Can action={UiAction.nav.employeeRoles}>
            <Link to={Routes.employeeRoles()} className="px-4 py-2 text-sm font-medium text-foreground/70 hover:text-foreground hover:bg-accent rounded-md transition-all">
              {Texts.employeeRoles}
            </Link>
          </Can>
        </nav>

        {/* Actions */}
        <div className="flex items-center gap-1">
          <Can action={UiAction.nav.employeeRoles}>
            <RoleViewSwitcher />
          </Can>
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon" className="text-muted-foreground hover:text-foreground hover:bg-accent">
                <User className="h-5 w-5" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuLabel>{currentUser.fullName}</DropdownMenuLabel>
              <DropdownMenuSeparator />
              <DropdownMenuItem
                onSelect={(event) => {
                  event.preventDefault();
                }}
                onClick={handleLogout}
              >
                {Texts.logout}
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </div>
    </header>
  );
};
