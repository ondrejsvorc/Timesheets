import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { DialogCancelButton, DialogConfirmButton } from "@/components/shared/buttons/DialogButtons";
import { ComboBox, type ComboBoxItem } from "@/components/shared/inputs/ComboBox";
import { DatePicker } from "@/components/shared/inputs/DatePicker";
import { WorkloadPercentInput } from "@/components/shared/inputs/WorkloadPercentInput";
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Form, FormControl, FormField, FormItem, FormLabel } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Texts } from "@/constants/texts";
import { getContractCatalog } from "@/features/employees/api/getContractCatalog";
import { getProjectCatalog } from "@/features/employees/api/getProjectCatalog";
import { parseCalendarDate } from "@/utils/calendarDate";
import { isWholeWorkloadPercentInRange, workloadPercentToFraction } from "@/utils/workloadPercentForm";
import { addContractEmployee } from "../contract/api/addContractEmployee";

const toIsoOrEmpty = (value: string | undefined) => (value && value.trim().length > 0 ? value : undefined);

type AddEmployeePositionFormValues = z.infer<typeof addEmployeePositionSchema>;
const addEmployeePositionSchema = z.object({
  projectId: z.string().nonempty(),
  contractId: z.string().nonempty(),
  positionCode: z.string().nonempty(),
  positionName: z.string().nonempty(),
  workload: z
    .string()
    .nonempty()
    .refine((v) => isWholeWorkloadPercentInRange(v, 0, 100), Texts.enterWholeNumberRange0to100),
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
  const [projects, setProjects] = useState<ComboBoxItem[]>([]);
  const [contracts, setContracts] = useState<ComboBoxItem[]>([]);
  const [projectsLoading, setProjectsLoading] = useState(false);
  const [contractsLoading, setContractsLoading] = useState(false);

  const form = useForm<AddEmployeePositionFormValues>({
    resolver: zodResolver(addEmployeePositionSchema),
    mode: "onChange",
  });

  const projectId = form.watch("projectId");
  const startDate = form.watch("startDate");
  const endDate = form.watch("endDate");

  useEffect(() => {
    if (!open) {
      return;
    }

    const controller = new AbortController();
    setProjectsLoading(true);
    getProjectCatalog()
      .then((response) =>
        setProjects(
          response.projects.map((project) => ({
            value: project.id,
            label: project.name,
          })),
        ),
      )
      .finally(() => setProjectsLoading(false));

    return () => controller.abort();
  }, [open]);

  useEffect(() => {
    if (!projectId) {
      setContracts([]);
      return;
    }

    const controller = new AbortController();
    setContractsLoading(true);
    getContractCatalog(projectId)
      .then((response) =>
        setContracts(
          response.contracts.map((contract) => ({
            value: contract.id,
            label: contract.name,
          })),
        ),
      )
      .finally(() => setContractsLoading(false));

    return () => controller.abort();
  }, [projectId]);

  const handleOpenChange = (isOpen: boolean) => {
    if (!isOpen) {
      handleClose();
    }
  };

  const handleProjectChange = (nextProjectId: string) => {
    form.setValue("projectId", nextProjectId);
    form.setValue("contractId", "");
  };

  const handleClose = () => {
    form.reset();
    onClose();
  };

  const handleSubmit = async (values: AddEmployeePositionFormValues, signal: AbortSignal) => {
    await addContractEmployee(
      values.contractId,
      {
        employeeId,
        positionCode: values.positionCode.trim(),
        position: values.positionName.trim(),
        workload: workloadPercentToFraction(values.workload),
        startDate: values.startDate,
        endDate: toIsoOrEmpty(values.endDate) ?? null,
      },
      signal,
    );

    onSaved();
    form.reset();
  };

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{Texts.addEmployeePositionToEmployeeTitle}</DialogTitle>
        </DialogHeader>

        <Form {...form}>
          <form className="space-y-4">
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
                      placeholder={Texts.selectProject}
                      loading={projectsLoading}
                      onChange={handleProjectChange}
                    />
                  </FormControl>
                </FormItem>
              )}
            />

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
                      placeholder={Texts.selectContract}
                      loading={contractsLoading}
                      disabled={!projectId}
                      onChange={field.onChange}
                    />
                  </FormControl>
                </FormItem>
              )}
            />

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

            <div className="grid grid-cols-2 gap-4">
              {(["startDate", "endDate"] as const).map((name) => (
                <FormField
                  key={name}
                  control={form.control}
                  name={name}
                  render={({ field }) => (
                    <FormItem className="flex flex-col">
                      <FormLabel>{name === "startDate" ? Texts.startDateRequiredLabel : Texts.endDateLabel}</FormLabel>
                      <FormControl>
                        <DatePicker
                          value={field.value}
                          clearable={name !== "startDate"}
                          disabledDate={(date) =>
                            name === "startDate"
                              ? endDate
                                ? date >= parseCalendarDate(endDate)
                                : false
                              : startDate
                                ? date <= parseCalendarDate(startDate)
                                : false
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
