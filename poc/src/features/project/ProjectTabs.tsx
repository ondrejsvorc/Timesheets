import { NavLink, useParams } from "react-router";
import { Routes } from "@/constants/routes";
import { cn } from "@/utils/cn";

export const ProjectTabs = () => {
  const { id } = useParams();

  if (!id) {
    return null;
  }

  return (
    <div className="border-b">
      <nav className="flex gap-6">
        <NavLink
          to={Routes.projectContracts(id)}
          end
          className={({ isActive }) =>
            cn(
              "pb-2 text-sm font-medium transition-colors",
              isActive ? "border-b-2 border-primary text-foreground" : "text-muted-foreground hover:text-foreground",
            )
          }
        >
          Zakázky
        </NavLink>
        <NavLink
          to={Routes.projectContractsManagers(id)}
          className={({ isActive }) =>
            cn(
              "pb-2 text-sm font-medium transition-colors",
              isActive ? "border-b-2 border-primary text-foreground" : "text-muted-foreground hover:text-foreground",
            )
          }
        >
          Manažeři zakázek
        </NavLink>
      </nav>
    </div>
  );
};
