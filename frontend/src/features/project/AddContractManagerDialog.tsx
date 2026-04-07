import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect, useMemo, useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { DialogCancelButton, DialogConfirmButton } from "@/components/shared/buttons/DialogButtons";
import { ComboBox, type ComboBoxItem } from "@/components/shared/inputs/ComboBox";
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Form, FormControl, FormField, FormItem, FormLabel } from "@/components/ui/form";
import { Texts } from "@/constants/texts";
import type { EmployeeItem } from "@/features/employees/api/getEmployees";
import { getEmployees } from "@/features/employees/api/getEmployees";
import { addContractManager, toProjectContractManagerItem } from "./api/addContractManager";
import { getProjectContracts } from "./api/getProjectContracts";
import type { ProjectContractManagerItem } from "./api/getProjectContractsManagers";

const schema = z.object({
  contractId: z.string().min(1, Texts.contract),
  employeeId: z.string().min(1, Texts.employees),
});
type FormValues = z.infer<typeof schema>;

interface AddContractManagerDialogProps {
  projectId: string;
  existingManagers: ProjectContractManagerItem[];
  open: boolean;
  onClose: () => void;
  onSaved: (manager: ProjectContractManagerItem) => void;
}

export const AddContractManagerDialog = ({ projectId, existingManagers, open, onClose, onSaved }: AddContractManagerDialogProps) => {
  const [contractItems, setContractItems] = useState<ComboBoxItem[]>([]);
  const [allEmployees, setAllEmployees] = useState<EmployeeItem[]>([]);
  const [loading, setLoading] = useState(false);

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    mode: "onChange",
    defaultValues: { contractId: "", employeeId: "" },
  });

  const selectedContractId = form.watch("contractId");

  const employeeItems = useMemo(() => {
    if (!selectedContractId) return [];
    const managerEmployeeIds = new Set(existingManagers.filter((m) => m.contractId === selectedContractId).map((m) => m.employeeId));
    return allEmployees.filter((e) => !managerEmployeeIds.has(e.id)).map((e) => ({ value: e.id, label: e.fullName }));
  }, [selectedContractId, existingManagers, allEmployees]);

  useEffect(() => {
    if (!open || !projectId) return;
    setLoading(true);
    Promise.all([getProjectContracts(projectId).promise, getEmployees().promise])
      .then(([contractsRes, employeesRes]) => {
        setContractItems(contractsRes.projectContracts.map((c) => ({ value: c.id, label: c.name })));
        setAllEmployees(employeesRes.employees);
      })
      .finally(() => setLoading(false));
  }, [open, projectId]);

  useEffect(() => {
    if (!selectedContractId) {
      form.setValue("employeeId", "");
    }
  }, [selectedContractId, form]);

  useEffect(() => {
    if (!open) {
      form.reset({ contractId: "", employeeId: "" });
    }
  }, [open, form]);

  const handleSubmit = async (values: FormValues, signal: AbortSignal) => {
    const response = await addContractManager(values.contractId, values.employeeId, signal);
    onSaved(toProjectContractManagerItem(response));
    onClose();
  };

  return (
    <Dialog open={open} onOpenChange={(isOpen) => !isOpen && onClose()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{Texts.addManager}</DialogTitle>
        </DialogHeader>
        <Form {...form}>
          <form className="space-y-4">
            <FormField
              control={form.control}
              name="contractId"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{Texts.contract}</FormLabel>
                  <FormControl>
                    <ComboBox value={field.value} items={contractItems} placeholder={Texts.contract} loading={loading} onChange={field.onChange} />
                  </FormControl>
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="employeeId"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{Texts.employees}</FormLabel>
                  <FormControl>
                    <ComboBox
                      value={field.value}
                      items={employeeItems}
                      placeholder={Texts.employees}
                      loading={loading}
                      disabled={!selectedContractId}
                      onChange={field.onChange}
                    />
                  </FormControl>
                </FormItem>
              )}
            />
            <DialogFooter>
              <DialogCancelButton onClick={onClose} />
              <DialogConfirmButton
                disabled={!form.watch("contractId") || !form.watch("employeeId")}
                onClick={(_, signal) => form.handleSubmit((values) => handleSubmit(values, signal))()}
              />
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  );
};
