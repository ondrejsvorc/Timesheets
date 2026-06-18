import { Archive, ArchiveRestore, MoreHorizontal } from "lucide-react";
import { useState } from "react";
import { Can } from "@/auth/Can";
import { UiAction } from "@/auth/uiPermissions";
import { DeleteIcon, EditIcon } from "@/components/shared/buttons/ActionButtons";
import { ConfirmationDialog } from "@/components/shared/dialogs/ConfirmationDialog";
import { Button } from "@/components/ui/button";
import { Card, CardAction, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import { Routes } from "@/constants/routes";
import { Texts } from "@/constants/texts";
import { useNavigateFrom } from "@/hooks/useNavigateFrom";
import { cn } from "@/utils/cn";
import { formatDate } from "@/utils/formatDate";
import { archiveProject, deleteProject, type ProjectItem, unarchiveProject } from "./api";
import { UpdateProjectDialog } from "./UpdateProjectDialog";
import { getProjectStatus } from "./utils/getProjectStatus";

interface ProjectCardProps {
  project: ProjectItem;
  onUpdate: (project: ProjectItem) => void;
  onDelete: (projectId: string) => void;
}

export const ProjectCard = ({ project, onUpdate, onDelete }: ProjectCardProps) => {
  const [isEditOpen, setIsEditOpen] = useState(false);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);
  const startDate = formatDate(project.startDate);
  const endDate = formatDate(project.endDate);
  const dateRange = project.startDate && project.endDate ? `${startDate} – ${endDate}` : formatDate(project.startDate);
  const status = getProjectStatus(project);
  const navigate = useNavigateFrom();

  return (
    <>
      <Card
        className="cursor-pointer group hover:border-primary/20 transition-all duration-200"
        onClick={() => {
          navigate(Routes.project(project.id));
        }}
      >
        <CardHeader>
          <CardTitle className="group-hover:text-primary transition-colors">{project.name}</CardTitle>
          {project.registrationNumber && <div className="text-sm text-muted-foreground font-mono">{project.registrationNumber}</div>}
          <CardAction>
            <Can action={UiAction.projects.edit} context={{ projectId: project.id }}>
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="ghost" size="icon" className="h-8 w-8 opacity-0 group-hover:opacity-100 transition-opacity">
                    <MoreHorizontal className="h-4 w-4" />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent>
                  <DropdownMenuItem
                    onClick={(e) => {
                      e.stopPropagation();
                      setIsEditOpen(true);
                    }}
                  >
                    <EditIcon />
                    {Texts.edit}
                  </DropdownMenuItem>
                  <DropdownMenuItem
                    onClick={async (e) => {
                      e.stopPropagation();
                      const updated = status === "archived" ? await unarchiveProject(project.id) : await archiveProject(project.id);
                      onUpdate(updated);
                    }}
                  >
                    {status === "archived" ? <ArchiveRestore className="h-4 w-4" /> : <Archive className="h-4 w-4" />}
                    {status === "archived" ? Texts.unarchive : Texts.archive}
                  </DropdownMenuItem>
                  <Can action={UiAction.projects.delete}>
                    <DropdownMenuItem
                      onClick={(e) => {
                        e.stopPropagation();
                        setIsConfirmOpen(true);
                      }}
                    >
                      <DeleteIcon />
                      {Texts.delete}
                    </DropdownMenuItem>
                  </Can>
                </DropdownMenuContent>
              </DropdownMenu>
            </Can>
          </CardAction>
        </CardHeader>
        <CardContent className="flex flex-col gap-3">
          <div className="text-sm text-muted-foreground">{dateRange}</div>
          <div className="text-sm text-foreground/90">
            {Texts.contracts}: <span className="font-medium">{project.contractCount}</span>
          </div>
          <div className="mt-auto pt-2">
            <span
              className={cn(
                "inline-flex items-center rounded-full px-3 py-1 text-xs font-medium transition-colors",
                status === "archived"
                  ? "bg-muted text-muted-foreground border border-border"
                  : status === "active"
                    ? "bg-primary/10 text-primary border border-primary/20"
                    : "bg-muted text-muted-foreground border border-border",
              )}
            >
              {status === "archived" ? Texts.archived : status === "active" ? Texts.active : Texts.inactive}
            </span>
          </div>
        </CardContent>
      </Card>
      <UpdateProjectDialog
        open={isEditOpen}
        project={project}
        onClose={() => setIsEditOpen(false)}
        onSaved={(updated) => {
          onUpdate(updated);
          setIsEditOpen(false);
        }}
      />
      {isConfirmOpen && (
        <ConfirmationDialog
          open
          onCancel={() => setIsConfirmOpen(false)}
          onConfirm={async (_event, signal) => {
            await deleteProject(project.id, signal);
            if (!signal.aborted) {
              onDelete(project.id);
              setIsConfirmOpen(false);
            }
          }}
        />
      )}
    </>
  );
};
