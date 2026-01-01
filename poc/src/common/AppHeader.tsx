import { Bell, User } from "lucide-react";
import { Link } from "react-router";
import { Button } from "@/components/ui/button";
import { Texts } from "./Texts";

export const AppHeader = () => {
  const handleNotificationsClick = () => {};
  const handleUserClick = () => {};

  return (
    <header className="w-full border-b border-primary/15 bg-background">
      <div className="mx-auto max-w-7xl flex h-14 items-center justify-between px-6">
        {/* Brand */}
        <Link to="/" className="text-lg font-semibold tracking-wide text-primary select-none">
          {Texts.applicationName}
        </Link>

        {/* Navigation */}
        <nav className="flex items-center gap-8">
          <Link to="/projects" className="text-foreground/80 hover:text-foreground transition-colors">
            {Texts.projects}
          </Link>
          <Link to="/employees" className="text-foreground/80 hover:text-foreground transition-colors">
            {Texts.employees}
          </Link>
        </nav>

        {/* Actions */}
        <div className="flex items-center gap-2">
          <Button variant="ghost" size="icon" onClick={handleNotificationsClick} className="text-muted-foreground hover:text-foreground">
            <Bell className="h-5 w-5" />
          </Button>

          <Button variant="ghost" size="icon" onClick={handleUserClick} className="text-muted-foreground hover:text-foreground">
            <User className="h-5 w-5" />
          </Button>
        </div>
      </div>
    </header>
  );
};
