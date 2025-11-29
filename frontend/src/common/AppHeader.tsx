import { Bell, User } from "lucide-react";
import { Texts } from "./Texts";

export const AppHeader = () => {
  const handleNotificationsClick = () => { };
  const handleUserClick = () => { };

  return (
    <header className="w-full border-b border-gray-300 bg-white">
      <div className="mx-auto max-w-7xl flex items-center justify-between h-14 px-6">

        {/* Brand */}
        <a href="/" className="text-xl font-semibold tracking-wide cursor-pointer select-none">
          {Texts.applicationName}
        </a>

        {/* Navigation */}
        <nav className="flex items-center gap-8">
          <a href="/projects" className="text-gray-800 hover:text-black">{Texts.projects}</a>
          <a href="/employees" className="text-gray-800 hover:text-black">{Texts.employees}</a>
        </nav>

        {/* Actions */}
        <div className="flex items-center gap-6">
          <button onClick={handleNotificationsClick} className="text-gray-700 hover:text-black"><Bell className="w-5 h-5" /></button>
          <button onClick={handleUserClick} className="text-gray-700 hover:text-black"><User className="w-5 h-5" /></button>
        </div>

      </div>
    </header>
  );
};
