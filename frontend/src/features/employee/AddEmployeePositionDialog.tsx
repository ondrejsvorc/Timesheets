import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect } from "react";
import { useForm } from "react-hook-form";
import { useFetcher } from "react-router";
import { z } from "zod";
import { DialogCancelButton, DialogConfirmButton } from "@/components/shared/buttons/DialogButtons";
import { ComboBox, type ComboBoxItem } from "@/components/shared/inputs/ComboBox";
import { DatePicker } from "@/components/shared/inputs/DatePicker";
import { WorkloadPercentInput } from "@/components/shared/inputs/WorkloadPercentInput";
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Form, FormControl, FormField, FormItem, FormLabel } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Routes } from "@/constants/routes";
import { Texts } from "@/constants/texts";
import { parseCalendarDate } from "@/utils/calendarDate";
import { isWholeWorkloadPercentInRange, workloadPercentToFraction } from "@/utils/workloadPercentForm";
import { addContractEmployee } from "../contract/api/addContractEmployee";
import type { GetContractCatalogResponse } from "../employees/api/getContractCatalog";
import type { GetProjectCatalogResponse } from "../employees/api/getProjectCatalog";

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
  const projectsFetcher = useFetcher<GetProjectCatalogResponse>();
  const contractsFetcher = useFetcher<GetContractCatalogResponse>();

  const form = useForm<AddEmployeePositionFormValues>({
    resolver: zodResolver(addEmployeePositionSchema),
    mode: "onChange",
  });

  const projectId = form.watch("projectId");
  const startDate = form.watch("startDate");
  const endDate = form.watch("endDate");

  const projects: ComboBoxItem[] =
    projectsFetcher.data?.projects.map((p) => ({
      value: p.id,
      label: p.name,
    })) ?? [];

  const contracts: ComboBoxItem[] =
    contractsFetcher.data?.contracts.map((c) => ({
      value: c.id,
      label: c.name,
    })) ?? [];

  useEffect(() => {
    if (!open) {
      return;
    }
    projectsFetcher.load(Routes.resourceProjects());
  }, [open, projectsFetcher]);

  const handleOpenChange = (isOpen: boolean) => {
    if (!isOpen) {
      handleClose();
    }
  };

  const handleProjectChange = (nextProjectId: string) => {
    form.setValue("projectId", nextProjectId);
    form.setValue("contractId", "");
    if (nextProjectId) {
      contractsFetcher.load(Routes.resourceContracts(nextProjectId));
    }
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
                      loading={projectsFetcher.state !== "idle"}
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
                      loading={contractsFetcher.state !== "idle"}
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
