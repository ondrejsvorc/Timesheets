import { NavLink, useParams } from "react-router";
import { Routes } from "@/constants/routes";
import { Texts } from "@/constants/texts";
import { cn } from "@/utils/cn";

export const ContractTabs = () => {
  const { id: projectId, contractId } = useParams();

  if (!projectId || !contractId) {
    return null;
  }

  return (
    <div className="border-b">
      <nav className="flex gap-6">
        <NavLink
          to={Routes.contractTimesheets(projectId, contractId)}
          end
          className={({ isActive }) =>
            cn(
              "pb-2 text-sm font-medium transition-colors",
              isActive ? "border-b-2 border-primary text-foreground" : "text-muted-foreground hover:text-foreground",
            )
          }
        >
          {Texts.timesheets}
        </NavLink>
        <NavLink
          to={Routes.contractEmployees(projectId, contractId)}
          className={({ isActive }) =>
            cn(
              "pb-2 text-sm font-medium transition-colors",
              isActive ? "border-b-2 border-primary text-foreground" : "text-muted-foreground hover:text-foreground",
            )
          }
        >
          {Texts.employees}
        </NavLink>
      </nav>
    </div>
  );
};
