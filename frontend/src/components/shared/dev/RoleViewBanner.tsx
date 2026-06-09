import { useEffectivePermissions } from "@/auth/RoleViewContext";
import { Texts } from "@/constants/texts";

const roleViewLabel = (mode: string) => {
  switch (mode) {
    case "employee":
      return Texts.roleViewEmployee;
    case "globalManager":
      return Texts.roleViewGlobalManager;
    case "projectManager":
      return Texts.roleViewProjectManager;
    case "contractManager":
      return Texts.roleViewContractManager;
    case "roleManager":
      return Texts.roleViewRoleManager;
    default:
      return mode;
  }
};

export const RoleViewBanner = () => {
  const { isOverridden, roleView } = useEffectivePermissions();

  if (!isOverridden) {
    return null;
  }

  return (
    <div className="border-b border-amber-500/30 bg-amber-500/10 px-6 py-2 text-center text-sm text-amber-950 dark:text-amber-100">
      {Texts.roleViewActive.replace("{role}", roleViewLabel(roleView.mode))}
    </div>
  );
};
