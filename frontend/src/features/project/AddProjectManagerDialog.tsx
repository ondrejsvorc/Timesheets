import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect, useMemo, useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { DialogCancelButton, DialogConfirmButton } from "@/components/shared/buttons/DialogButtons";
import { ComboBox } from "@/components/shared/inputs/ComboBox";
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Form, FormControl, FormField, FormItem, FormLabel } from "@/components/ui/form";
import { Texts } from "@/constants/texts";
import { type EmployeeItem, getEmployees } from "@/features/employees/api";
import { addProjectManager, type ProjectManagerItem } from "./api";

const schema = z.object({
  employeeId: z.string().min(1, Texts.employees),
});
type FormValues = z.infer<typeof schema>;

interface AddProjectManagerDialogProps {
  projectId: string;
  existingManagers: ProjectManagerItem[];
  open: boolean;
  onClose: () => void;
  onSaved: (manager: ProjectManagerItem) => void;
}

export const AddProjectManagerDialog = ({ projectId, existingManagers, open, onClose, onSaved }: AddProjectManagerDialogProps) => {
  const [employees, setEmployees] = useState<EmployeeItem[]>([]);
  const [loading, setLoading] = useState(false);

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    mode: "onChange",
    defaultValues: { employeeId: "" },
  });

  useEffect(() => {
    if (!open) {
      return;
    }

    setLoading(true);
    getEmployees()
      .then((response) => setEmployees(response.employees))
      .finally(() => setLoading(false));
  }, [open]);

  const employeeItems = useMemo(() => {
    const managerEmployeeIds = new Set(existingManagers.map((m) => m.employeeId));
    return employees.filter((e) => !managerEmployeeIds.has(e.id)).map((e) => ({ value: e.id, label: e.fullName, searchText: e.personalNumber }));
  }, [existingManagers, employees]);

  const handleOpenChange = (isOpen: boolean) => {
    if (!isOpen) {
      form.reset({ employeeId: "" });
      onClose();
    }
  };

  const handleSubmit = async (values: FormValues, signal: AbortSignal) => {
    const response = await addProjectManager(projectId, values.employeeId, signal);
    onSaved(response);
    onClose();
  };

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{Texts.addManager}</DialogTitle>
        </DialogHeader>
        <Form {...form}>
          <form className="space-y-4">
            <FormField
              control={form.control}
              name="employeeId"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{Texts.employees}</FormLabel>
                  <FormControl>
                    <ComboBox value={field.value} items={employeeItems} placeholder={Texts.employees} loading={loading} onChange={field.onChange} />
                  </FormControl>
                </FormItem>
              )}
            />
            <DialogFooter>
              <DialogCancelButton onClick={onClose} />
              <DialogConfirmButton disabled={!form.watch("employeeId")} onClick={(_, signal) => form.handleSubmit((values) => handleSubmit(values, signal))()} />
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  );
};
