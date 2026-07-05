import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { FormDialog } from "@/components/shared/dialogs/FormDialog";
import { Texts } from "@/constants/texts";
import { type ProjectItem, updateProject } from "./api";
import { ProjectFormFields, type ProjectFormValues, projectFormSchema } from "./ProjectFormFields";

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
  if (!project) {
    return null;
  }

  const defaultValues = projectToFormValues(project);
  const form = useForm<ProjectFormValues>({ defaultValues, resolver: zodResolver(projectFormSchema), mode: "onChange" });

  const handleSubmit = async (values: ProjectFormValues, signal: AbortSignal) => {
    const response = await updateProject(project.id, values, signal);
    onSaved(response.project);
    form.reset(projectToFormValues(response.project));
    onClose();
  };

  const handleClose = () => {
    form.reset(defaultValues);
    onClose();
  };

  return (
    <FormDialog open={open} title={Texts.editProject} onClose={handleClose}>
      <ProjectFormFields form={form} onSubmit={handleSubmit} onCancel={handleClose} />
    </FormDialog>
  );
};
