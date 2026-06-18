import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { FormDialog } from "@/components/shared/dialogs/FormDialog";
import { Texts } from "@/constants/texts";
import { type CreateProjectRequest, createProject, type ProjectItem } from "./api";
import { ProjectFormFields, type ProjectFormValues, projectFormDefaultValues, projectFormSchema } from "./ProjectFormFields";

interface AddProjectDialogProps {
  open: boolean;
  onClose: () => void;
  onSaved: (project: ProjectItem) => void;
}

export const AddProjectDialog = ({ open, onClose, onSaved }: AddProjectDialogProps) => {
  const form = useForm<ProjectFormValues>({
    defaultValues: projectFormDefaultValues,
    resolver: zodResolver(projectFormSchema),
    mode: "onChange",
  });

  const handleClose = () => {
    form.reset(projectFormDefaultValues);
    onClose();
  };

  const handleSubmit = async (values: ProjectFormValues, signal: AbortSignal) => {
    const request: CreateProjectRequest = {
      name: values.name,
      registrationNumber: values.registrationNumber,
      startDate: values.startDate,
      endDate: values.endDate ?? null,
    };
    const response = await createProject(request, signal);
    onSaved(response.project);
    form.reset(projectFormDefaultValues);
  };

  return (
    <FormDialog open={open} title={Texts.newProject} onClose={handleClose}>
      <ProjectFormFields form={form} onSubmit={handleSubmit} onCancel={handleClose} />
    </FormDialog>
  );
};
