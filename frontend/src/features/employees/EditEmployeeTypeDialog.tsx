import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect, useMemo } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { DialogCancelButton, DialogConfirmButton } from "@/components/shared/buttons/DialogButtons";
import { ComboBox, type ComboBoxItem } from "@/components/shared/inputs/ComboBox";
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Form, FormControl, FormField, FormItem, FormLabel } from "@/components/ui/form";
import { EmployeeTypeAcademicId, EmployeeTypeNonAcademicId } from "@/constants/api";
import { Texts } from "@/constants/texts";
import type { EmployeeItem } from "./api/getEmployees";
import { updateEmployeeType } from "./api/updateEmployeeType";

type FormValues = z.infer<typeof schema>;

const schema = z.object({
  employeeTypeId: z.string(),
});

interface EditEmployeeTypeDialogProps {
  open: boolean;
  employee: EmployeeItem;
  onClose: () => void;
  onSaved: (nextEmployeeTypeId: string | null) => void;
}

export const EditEmployeeTypeDialog = ({ open, employee, onClose, onSaved }: EditEmployeeTypeDialogProps) => {
  const items: ComboBoxItem[] = useMemo(
    () => [
      { value: "", label: "" },
      { value: EmployeeTypeAcademicId, label: Texts.academic },
      { value: EmployeeTypeNonAcademicId, label: Texts.nonAcademic },
    ],
    [],
  );

  const form = useForm<FormValues>({
    defaultValues: { employeeTypeId: employee.employeeTypeId ?? "" },
    resolver: zodResolver(schema),
    mode: "onChange",
  });

  useEffect(() => {
    if (!open) return;
    form.reset({ employeeTypeId: employee.employeeTypeId ?? "" });
  }, [open, employee.employeeTypeId, form]);

  const handleClose = () => {
    form.reset({ employeeTypeId: employee.employeeTypeId ?? "" });
    onClose();
  };

  const handleSubmit = async (values: FormValues, signal: AbortSignal) => {
    const nextEmployeeTypeId = values.employeeTypeId.trim().length === 0 ? null : values.employeeTypeId;
    await updateEmployeeType(employee.id, { employeeTypeId: nextEmployeeTypeId }, signal);
    onSaved(nextEmployeeTypeId);
    onClose();
  };

  return (
    <Dialog open={open} onOpenChange={(isOpen) => !isOpen && handleClose()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{Texts.employeeType}</DialogTitle>
        </DialogHeader>

        <Form {...form}>
          <form className="space-y-4">
            <FormField
              control={form.control}
              name="employeeTypeId"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{Texts.employeeType}</FormLabel>
                  <FormControl>
                    <ComboBox value={field.value} items={items} placeholder="" onChange={field.onChange} />
                  </FormControl>
                </FormItem>
              )}
            />

            <DialogFooter>
              <DialogCancelButton onClick={handleClose} />
              <DialogConfirmButton onClick={(_, signal) => form.handleSubmit((values) => handleSubmit(values, signal))()} />
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  );
};

