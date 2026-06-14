import { NavLink, useParams } from "react-router";
import { Can } from "@/auth/Can";
import { UiAction } from "@/auth/uiPermissions";
import { Routes } from "@/constants/routes";
import { Texts } from "@/constants/texts";
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
          className={({ isActive }) => cn("pb-2 text-sm font-medium transition-colors", isActive ? "border-b-2 border-primary text-foreground" : "text-muted-foreground hover:text-foreground")}
        >
          {Texts.contracts}
        </NavLink>
        <Can action={UiAction.contractManagers.view} context={{ projectId: id }}>
          <NavLink
            to={Routes.projectContractsManagers(id)}
            className={({ isActive }) => cn("pb-2 text-sm font-medium transition-colors", isActive ? "border-b-2 border-primary text-foreground" : "text-muted-foreground hover:text-foreground")}
          >
            {Texts.contractsManagers}
          </NavLink>
        </Can>
      </nav>
    </div>
  );
};
