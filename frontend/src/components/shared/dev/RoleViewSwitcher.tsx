import { Eye } from "lucide-react";
import { useState } from "react";
import { useFetcher } from "react-router";
import { useEffectivePermissions } from "@/auth/RoleViewContext";
import type { RoleViewMode } from "@/auth/roleView";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Routes } from "@/constants/routes";
import { Texts } from "@/constants/texts";
import type { GetContractCatalogResponse } from "@/features/employees/api/getContractCatalog";
import type { GetProjectCatalogResponse } from "@/features/employees/api/getProjectCatalog";
import { cn } from "@/utils/cn";

const roleViewModeOptions: { value: RoleViewMode; label: string }[] = [
  { value: "actual", label: Texts.roleViewActual },
  { value: "employee", label: Texts.roleViewEmployee },
  { value: "globalManager", label: Texts.roleViewGlobalManager },
  { value: "projectManager", label: Texts.roleViewProjectManager },
  { value: "contractManager", label: Texts.roleViewContractManager },
  { value: "roleManager", label: Texts.roleViewRoleManager },
];

export const RoleViewSwitcher = () => {
  const { roleView, setRoleView, isOverridden } = useEffectivePermissions();
  const [open, setOpen] = useState(false);
  const projectsFetcher = useFetcher<GetProjectCatalogResponse>();
  const contractsFetcher = useFetcher<GetContractCatalogResponse>();

  const projects = projectsFetcher.data?.projects ?? [];
  const contracts = contractsFetcher.data?.contracts ?? [];

  const handleOpenChange = (nextOpen: boolean) => {
    setOpen(nextOpen);
    if (nextOpen && projectsFetcher.state === "idle") {
      projectsFetcher.load(Routes.resourceProjects());
    }
  };

  const handleProjectChange = (projectId: string, resetContract: boolean) => {
    setRoleView({
      ...roleView,
      projectId,
      contractId: resetContract ? null : roleView.contractId,
    });
    if (roleView.mode === "contractManager" && projectId) {
      contractsFetcher.load(Routes.resourceContracts(projectId));
    }
  };

  const selectedMode = roleViewModeOptions.find((option) => option.value === roleView.mode);

  return (
    <Popover open={open} onOpenChange={handleOpenChange}>
      <PopoverTrigger asChild>
        <Button
          type="button"
          variant={isOverridden ? "secondary" : "ghost"}
          size="sm"
          className={cn("gap-2", isOverridden && "border border-amber-500/50 bg-amber-500/10 text-amber-900 dark:text-amber-100")}
        >
          <Eye className="size-4" />
          <span className="hidden sm:inline">{Texts.roleView}</span>
          {isOverridden && selectedMode && <span className="hidden md:inline text-xs opacity-80">({selectedMode.label})</span>}
        </Button>
      </PopoverTrigger>
      <PopoverContent align="end" side="bottom" className="z-[100] w-80 space-y-4">
        <div className="space-y-1">
          <p className="text-sm font-medium">{Texts.roleView}</p>
          <p className="text-xs text-muted-foreground">{Texts.roleViewDescription}</p>
        </div>

        <div className="space-y-2">
          <Label htmlFor="role-view-mode">{Texts.roleViewMode}</Label>
          <Select
            value={roleView.mode}
            onValueChange={(mode) =>
              setRoleView({
                mode: mode as RoleViewMode,
                projectId: null,
                contractId: null,
              })
            }
          >
            <SelectTrigger id="role-view-mode" className="w-full">
              <SelectValue />
            </SelectTrigger>
            <SelectContent className="z-[110]">
              {roleViewModeOptions.map((option) => (
                <SelectItem key={option.value} value={option.value}>
                  {option.label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        {roleView.mode === "projectManager" && (
          <div className="space-y-2">
            <Label htmlFor="role-view-project">{Texts.project}</Label>
            <Select value={roleView.projectId ?? ""} onValueChange={(projectId) => handleProjectChange(projectId, true)}>
              <SelectTrigger id="role-view-project" className="w-full">
                <SelectValue placeholder={Texts.selectProject} />
              </SelectTrigger>
              <SelectContent className="z-[110]">
                {projects.map((project) => (
                  <SelectItem key={project.id} value={project.id}>
                    {project.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        )}

        {roleView.mode === "contractManager" && (
          <>
            <div className="space-y-2">
              <Label htmlFor="role-view-contract-project">{Texts.project}</Label>
              <Select value={roleView.projectId ?? ""} onValueChange={(projectId) => handleProjectChange(projectId, true)}>
                <SelectTrigger id="role-view-contract-project" className="w-full">
                  <SelectValue placeholder={Texts.selectProject} />
                </SelectTrigger>
                <SelectContent className="z-[110]">
                  {projects.map((project) => (
                    <SelectItem key={project.id} value={project.id}>
                      {project.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label htmlFor="role-view-contract">{Texts.contract}</Label>
              <Select
                value={roleView.contractId ?? ""}
                onValueChange={(contractId) => setRoleView({ ...roleView, contractId })}
                disabled={!roleView.projectId}
              >
                <SelectTrigger id="role-view-contract" className="w-full">
                  <SelectValue placeholder={Texts.selectContract} />
                </SelectTrigger>
                <SelectContent className="z-[110]">
                  {contracts.map((contract) => (
                    <SelectItem key={contract.id} value={contract.id}>
                      {contract.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </>
        )}
      </PopoverContent>
    </Popover>
  );
};
