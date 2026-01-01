import { Bell, User } from "lucide-react";
import { Link } from "react-router";
import { Button } from "@/components/ui/button";
import { Texts } from "./Texts";

export const AppHeader = () => {
  const handleNotificationsClick = () => {};
  const handleUserClick = () => {};

  return (
    <header className="w-full border-b border-border/50 bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60 sticky top-0 z-50">
      <div className="mx-auto max-w-7xl flex h-16 items-center justify-between px-6">
        {/* Brand */}
        <Link to="/" className="text-xl font-semibold tracking-tight text-primary select-none hover:text-primary/90 transition-colors">
          {Texts.applicationName}
        </Link>

        {/* Navigation */}
        <nav className="flex items-center gap-1">
          <Link to="/projects" className="px-4 py-2 text-sm font-medium text-foreground/70 hover:text-foreground hover:bg-accent rounded-md transition-all">
            {Texts.projects}
          </Link>
          <Link to="/employees" className="px-4 py-2 text-sm font-medium text-foreground/70 hover:text-foreground hover:bg-accent rounded-md transition-all">
            {Texts.employees}
          </Link>
        </nav>

        {/* Actions */}
        <div className="flex items-center gap-1">
          <Button variant="ghost" size="icon" onClick={handleNotificationsClick} className="text-muted-foreground hover:text-foreground hover:bg-accent">
            <Bell className="h-5 w-5" />
          </Button>

          <Button variant="ghost" size="icon" onClick={handleUserClick} className="text-muted-foreground hover:text-foreground hover:bg-accent">
            <User className="h-5 w-5" />
          </Button>
        </div>
      </div>
    </header>
  );
};
