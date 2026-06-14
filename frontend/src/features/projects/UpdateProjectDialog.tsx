import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Texts } from "@/constants/texts";
import type { ProjectItem } from "./api/shared/projectItem";
import { updateProject } from "./api/updateProject";
import { ProjectFormFields, type ProjectFormValues, projectFormDefaultValues, projectFormSchema } from "./ProjectFormFields";

interface UpdateProjectDialogProps {
  open: boolean;
  project: ProjectItem | null;
  onClose: () => void;
  onSaved: (project: ProjectItem) => void;
}

const projectToFormValues = (project: ProjectItem): ProjectFormValues => ({
  name: project.name,
  registrationNumber: project.registrationNumber,
  startDate: project.startDate,
  endDate: project.endDate ?? undefined,
});

export const UpdateProjectDialog = ({ open, project, onClose, onSaved }: UpdateProjectDialogProps) => {
  const defaultValues: ProjectFormValues = project ? projectToFormValues(project) : projectFormDefaultValues;
  const form = useForm<ProjectFormValues>({
    defaultValues,
    resolver: zodResolver(projectFormSchema),
    mode: "onChange",
  });

  const handleClose = () => {
    form.reset(defaultValues);
    onClose();
  };

  const handleSubmit = async (values: ProjectFormValues, signal: AbortSignal) => {
    if (!project) return;
    const response = await updateProject(project.id, values, signal);
    onSaved(response.project);
    form.reset(projectToFormValues(response.project));
    onClose();
  };

  if (!project) return null;

  return (
    <Dialog open={open} onOpenChange={(isOpen) => !isOpen && handleClose()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{Texts.editProject}</DialogTitle>
        </DialogHeader>
        <ProjectFormFields form={form} onSubmit={handleSubmit} onCancel={handleClose} />
      </DialogContent>
    </Dialog>
  );
};
