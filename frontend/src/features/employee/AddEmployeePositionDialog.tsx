import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect } from "react";
import { useForm } from "react-hook-form";
import { useImmer } from "use-immer";
import { z } from "zod";
import { DialogCancelButton, DialogConfirmButton } from "@/components/shared/buttons/DialogButtons";
import { ComboBox, type ComboBoxItem } from "@/components/shared/inputs/ComboBox";
import { DatePicker } from "@/components/shared/inputs/DatePicker";
import { WorkloadPercentInput } from "@/components/shared/inputs/WorkloadPercentInput";
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Form, FormControl, FormField, FormItem, FormLabel } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { isWholeWorkloadPercentInRange } from "@/utils/workloadPercentForm";
import { getContractCatalog } from "../employees/api/getContractCatalog";
import { getProjectCatalog } from "../employees/api/getProjectCatalog";

type AddEmployeePositionFormValues = z.infer<typeof addEmployeePositionSchema>;
const addEmployeePositionSchema = z.object({
  projectId: z.string().nonempty(),
  contractId: z.string().nonempty(),
  positionCode: z.string().nonempty(),
  positionName: z.string().nonempty(),
  workload: z
    .string()
    .nonempty()
    .refine((v) => isWholeWorkloadPercentInRange(v, 0, 100), "Zadejte celé číslo 0–100 (procenta úvazku)."),
  startDate: z.string().nonempty(),
  endDate: z.string().optional(),
});

interface AddEmployeePositionDialogProps {
  open: boolean;
  employeeId: string;
  onClose: () => void;
  onSaved: () => void;
}

export const AddEmployeePositionDialog = ({ open, employeeId, onClose, onSaved }: AddEmployeePositionDialogProps) => {
  const [projects, setProjects] = useImmer<ComboBoxItem[]>([]);
  const [contracts, setContracts] = useImmer<ComboBoxItem[]>([]);
  const [projectsLoading, setProjectsLoading] = useImmer(false);
  const [contractsLoading, setContractsLoading] = useImmer(false);

  const form = useForm<AddEmployeePositionFormValues>({
    resolver: zodResolver(addEmployeePositionSchema),
    mode: "onChange",
  });

  const projectId = form.watch("projectId");
  const startDate = form.watch("startDate");
  const endDate = form.watch("endDate");

  // Load projects on open
  useEffect(() => {
    if (!open) {
      return;
    }

    const loadProjects = async () => {
      setProjectsLoading(true);

      const response = await getProjectCatalog();
      setProjects(
        response.projects.map((p) => ({
          value: p.id,
          label: p.name,
        })),
      );

      setProjectsLoading(false);
    };

    loadProjects();
  }, [open, setProjects, setProjectsLoading]);

  // Load contracts on project change
  useEffect(() => {
    if (!projectId) {
      setContracts([]);
      return;
    }

    const loadContracts = async () => {
      setContractsLoading(true);
      setContracts([]);

      form.setValue("contractId", "");

      const response = await getContractCatalog(projectId);
      setContracts(
        response.contracts.map((c) => ({
          value: c.id,
          label: c.name,
        })),
      );

      setContractsLoading(false);
    };

    loadContracts();
  }, [projectId, form, setContracts, setContractsLoading]);

  const handleClose = () => {
    form.reset();
    setContracts([]);
    onClose();
  };

  const handleSubmit = async (_values: AddEmployeePositionFormValues, _signal: AbortSignal) => {
    void employeeId;
    onSaved();
    form.reset();
    setContracts([]);
  };

  return (
    <Dialog open={open} onOpenChange={(isOpen) => !isOpen && handleClose()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Přidat zaměstnanci pozici</DialogTitle>
        </DialogHeader>

        <Form {...form}>
          <form className="space-y-4">
            {/* Projekt */}
            <FormField
              control={form.control}
              name="projectId"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Projekt *</FormLabel>
                  <FormControl>
                    <ComboBox
                      value={field.value}
                      items={projects}
                      placeholder="Vyberte projekt"
                      loading={projectsLoading}
                      onChange={field.onChange}
                    />
                  </FormControl>
                </FormItem>
              )}
            />

            {/* Zakázka */}
            <FormField
              control={form.control}
              name="contractId"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Zakázka *</FormLabel>
                  <FormControl>
                    <ComboBox
                      value={field.value}
                      items={contracts}
                      placeholder="Vyberte zakázku"
                      loading={contractsLoading}
                      onChange={field.onChange}
                    />
                  </FormControl>
                </FormItem>
              )}
            />

            {/* Kód + název pozice */}
            <div className="grid grid-cols-2 gap-4">
              <FormField
                control={form.control}
                name="positionCode"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Kód pozice *</FormLabel>
                    <FormControl>
                      <Input {...field} />
                    </FormControl>
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="positionName"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Název pozice *</FormLabel>
                    <FormControl>
                      <Input {...field} />
                    </FormControl>
                  </FormItem>
                )}
              />
            </div>

            {/* Úvazek */}
            <FormField
              control={form.control}
              name="workload"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Úvazek *</FormLabel>
                  <FormControl>
                    <WorkloadPercentInput {...field} />
                  </FormControl>
                </FormItem>
              )}
            />

            {/* Datum začátku / ukončení */}
            <div className="grid grid-cols-2 gap-4">
              {(["startDate", "endDate"] as const).map((name) => (
                <FormField
                  key={name}
                  control={form.control}
                  name={name}
                  render={({ field }) => (
                    <FormItem className="flex flex-col">
                      <FormLabel>{name === "startDate" ? "Datum začátku *" : "Datum ukončení"}</FormLabel>
                      <FormControl>
                        <DatePicker
                          value={field.value}
                          clearable={name !== "startDate"}
                          disabledDate={(date) =>
                            name === "startDate" ? (endDate ? date >= new Date(endDate) : false) : startDate ? date <= new Date(startDate) : false
                          }
                          onChange={(next) => field.onChange(next ?? (name === "startDate" ? "" : undefined))}
                        />
                      </FormControl>
                    </FormItem>
                  )}
                />
              ))}
            </div>

            <DialogFooter>
              <DialogCancelButton onClick={handleClose} />
              <DialogConfirmButton
                disabled={!form.formState.isValid}
                onClick={(_, signal) => form.handleSubmit((values) => handleSubmit(values, signal))()}
              />
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  );
};
