import { Eye } from "lucide-react";
import { useEffect, useState } from "react";
import { useEffectivePermissions } from "@/auth/RoleViewContext";
import type { RoleViewMode } from "@/auth/roleView";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Texts } from "@/constants/texts";
import { type ContractCatalogItem, getContractCatalog } from "@/features/employees/api/getContractCatalog";
import { getProjects } from "@/features/projects/api/getProjects";
import type { ProjectItem } from "@/features/projects/api/shared/projectItem";
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
  const [projects, setProjects] = useState<ProjectItem[]>([]);
  const [contracts, setContracts] = useState<ContractCatalogItem[]>([]);

  useEffect(() => {
    if (!open) return;

    getProjects()
      .promise.then((response) => setProjects(response.projects))
      .catch(() => setProjects([]));
  }, [open]);

  useEffect(() => {
    if (!open || roleView.mode !== "contractManager" || !roleView.projectId) {
      setContracts([]);
      return;
    }

    let cancelled = false;
    getContractCatalog(roleView.projectId)
      .then((response) => {
        if (!cancelled) setContracts(response.contracts);
      })
      .catch(() => {
        if (!cancelled) setContracts([]);
      });

    return () => {
      cancelled = true;
    };
  }, [open, roleView.mode, roleView.projectId]);

  const selectedMode = roleViewModeOptions.find((option) => option.value === roleView.mode);

  return (
    <Popover open={open} onOpenChange={setOpen}>
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
            <Select value={roleView.projectId ?? ""} onValueChange={(projectId) => setRoleView({ ...roleView, projectId, contractId: null })}>
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
              <Select value={roleView.projectId ?? ""} onValueChange={(projectId) => setRoleView({ ...roleView, projectId, contractId: null })}>
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
