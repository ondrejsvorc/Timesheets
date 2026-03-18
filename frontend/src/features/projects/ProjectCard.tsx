import { DeleteIcon, EditIcon } from "@/components/shared/buttons/ActionButtons";
import { ConfirmationDialog } from "@/components/shared/dialogs/ConfirmationDialog";
import { Button } from "@/components/ui/button";
import { Card, CardAction, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import { Routes } from "@/constants/routes";
import { Texts } from "@/constants/texts";
import { cn } from "@/utils/cn";
import { formatDate } from "@/utils/formatDate";
import { MoreHorizontal } from "lucide-react";
import { startTransition } from "react";
import { useNavigate } from "react-router";
import { useImmer } from "use-immer";
import type { ProjectItem } from "./api/shared/projectItem";
import { deleteProject } from "./api/deleteProject";
import { useProjectsDispatch } from "./hooks/useProjectsDispatch";
import { UpdateProjectDialog } from "./UpdateProjectDialog";
import { isProjectActive } from "./utils/isProjectActive";

interface ProjectCardProps {
  project: ProjectItem;
}

export const ProjectCard = ({ project }: ProjectCardProps) => {
  const dispatch = useProjectsDispatch();
  const [isEditOpen, setIsEditOpen] = useImmer(false);
  const [isConfirmOpen, setIsConfirmOpen] = useImmer(false);
  const startDate = formatDate(project.startDate);
  const endDate = formatDate(project.endDate);
  const dateRange = project.startDate && project.endDate ? `${startDate} – ${endDate}` : formatDate(project.startDate);
  const isActive = isProjectActive(project);
  const navigate = useNavigate();

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
                    startTransition(() => setIsEditOpen(true));
                  }}
                >
                  <EditIcon />
                  {Texts.edit}
                </DropdownMenuItem>
                <DropdownMenuItem
                  onClick={(e) => {
                    e.stopPropagation();
                    startTransition(() => setIsConfirmOpen(true));
                  }}
                >
                  <DeleteIcon />
                  {Texts.delete}
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
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
                isActive ? "bg-primary/10 text-primary border border-primary/20" : "bg-muted text-muted-foreground border border-border",
              )}
            >
              {isActive ? Texts.active : Texts.inactive}
            </span>
          </div>
        </CardContent>
      </Card>
      <UpdateProjectDialog
        open={isEditOpen}
        project={project}
        onClose={() => setIsEditOpen(false)}
        onSaved={(updated) => {
          dispatch({ type: "update", project: updated });
          setIsEditOpen(false);
        }}
      />
      <ConfirmationDialog
        open={isConfirmOpen}
        onCancel={() => setIsConfirmOpen(false)}
        onConfirm={async (_event, signal) => {
          await deleteProject(project.id, signal);
          if (!signal.aborted) {
            dispatch({ type: "delete", projectId: project.id });
            setIsConfirmOpen(false);
          }
        }}
      />
    </>
  );
};
