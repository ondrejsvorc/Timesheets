import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect, useMemo, useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { DialogCancelButton, DialogConfirmButton } from "@/components/shared/buttons/DialogButtons";
import { ComboBox, type ComboBoxItem } from "@/components/shared/inputs/ComboBox";
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Form, FormControl, FormField, FormItem, FormLabel } from "@/components/ui/form";
import { Texts } from "@/constants/texts";
import { type EmployeeItem, getEmployees } from "@/features/employees/api/getEmployees";
import { addContractManager, toProjectContractManagerItem } from "./api/addContractManager";
import { getProjectContracts } from "./api/getProjectContracts";
import type { ProjectContractManagerItem } from "./api/getProjectContractsManagers";
import type { ProjectContractItem } from "./api/shared/projectContractItem";

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
  const [contracts, setContracts] = useState<ProjectContractItem[]>([]);
  const [employees, setEmployees] = useState<EmployeeItem[]>([]);
  const [loading, setLoading] = useState(false);

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    mode: "onChange",
    defaultValues: { contractId: "", employeeId: "" },
  });

  const selectedContractId = form.watch("contractId");

  useEffect(() => {
    if (!open) {
      return;
    }

    setLoading(true);
    Promise.all([getProjectContracts(projectId), getEmployees()])
      .then(([contractsResponse, employeesResponse]) => {
        setContracts(contractsResponse.projectContracts);
        setEmployees(employeesResponse.employees);
      })
      .finally(() => setLoading(false));
  }, [open, projectId]);

  const contractItems: ComboBoxItem[] = contracts.map((contract) => ({
    value: contract.id,
    label: contract.registrationNumber,
  }));

  const employeeItems = useMemo(() => {
    if (!selectedContractId) return [];
    const managerEmployeeIds = new Set(existingManagers.filter((m) => m.contractId === selectedContractId).map((m) => m.employeeId));
    return employees.filter((e) => !managerEmployeeIds.has(e.id)).map((e) => ({ value: e.id, label: e.fullName }));
  }, [selectedContractId, existingManagers, employees]);

  const handleOpenChange = (isOpen: boolean) => {
    if (!isOpen) {
      form.reset({ contractId: "", employeeId: "" });
      onClose();
    }
  };

  const handleContractChange = (contractId: string) => {
    form.setValue("contractId", contractId);
    form.setValue("employeeId", "");
  };

  const handleSubmit = async (values: FormValues, signal: AbortSignal) => {
    const response = await addContractManager(values.contractId, values.employeeId, signal);
    onSaved(toProjectContractManagerItem(response));
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
              name="contractId"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{Texts.contract}</FormLabel>
                  <FormControl>
                    <ComboBox value={field.value} items={contractItems} placeholder={Texts.contract} loading={loading} onChange={handleContractChange} />
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
                    <ComboBox value={field.value} items={employeeItems} placeholder={Texts.employees} loading={loading} disabled={!selectedContractId} onChange={field.onChange} />
                  </FormControl>
                </FormItem>
              )}
            />
            <DialogFooter>
              <DialogCancelButton onClick={onClose} />
              <DialogConfirmButton disabled={!form.watch("contractId") || !form.watch("employeeId")} onClick={(_, signal) => form.handleSubmit((values) => handleSubmit(values, signal))()} />
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  );
};
