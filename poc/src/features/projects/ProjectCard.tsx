import { format, parseISO } from "date-fns";
import { cs } from "date-fns/locale";
import { MoreHorizontal, Trash2 } from "lucide-react";
import { useImmer } from "use-immer";
import { ConfirmationDialog } from "@/common/ConfirmationDialog";
import { Texts } from "@/common/Texts";
import { Button } from "@/components/ui/button";
import { Card, CardAction, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import { cn } from "@/lib/utils";
import type { ProjectItem } from "./api/shared/projectItem";
import { useProjectsDispatch } from "./hooks/useProjectsDispatch";
import { isProjectActive } from "./utils/isProjectActive";

interface ProjectCardProps {
  project: ProjectItem;
}

export const ProjectCard = ({ project }: ProjectCardProps) => {
  const dispatch = useProjectsDispatch();
  const [isConfirmOpen, setIsConfirmOpen] = useImmer(false);
  const startDate = project.startDate ? format(parseISO(project.startDate), "d. M. yyyy", { locale: cs }) : null;
  const endDate = project.endDate ? format(parseISO(project.endDate), "d. M. yyyy", { locale: cs }) : null;
  const dateRange = startDate && endDate ? `${startDate} – ${endDate}` : startDate || Texts.noDate;
  const isActive = isProjectActive(project);

  return (
    <>
      <Card className="cursor-pointer group hover:border-primary/20 transition-all duration-200" onClick={() => {}}>
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
              <DropdownMenuContent align="end">
                <DropdownMenuItem onClick={() => setIsConfirmOpen(true)}>
                  <Trash2 />
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
                isActive
                  ? "bg-primary/10 text-primary border border-primary/20"
                  : "bg-muted text-muted-foreground border border-border",
              )}
            >
              {isActive ? Texts.active : Texts.inactive}
            </span>
          </div>
        </CardContent>
      </Card>
      <ConfirmationDialog
        open={isConfirmOpen}
        onCancel={() => {
          setIsConfirmOpen(false);
        }}
        onConfirm={async (_event, signal) => {
          dispatch({ type: "delete", projectId: project.id });
          await Promise.resolve();
          if (!signal.aborted) {
            setIsConfirmOpen(false);
          }
        }}
      />
    </>
  );
};
