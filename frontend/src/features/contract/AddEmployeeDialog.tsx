import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect, useMemo } from "react";
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
import type { GetEmployeesResponse } from "../employees/api/getEmployees";
import { addContractEmployee } from "./api/addContractEmployee";
import type { EmployeeItem as ContractEmployeeItem } from "./api/getContractEmployees";

type AddEmployeeToContractFormValues = z.infer<ReturnType<typeof createSchema>>;

const normalize = (value: string) => value.trim().replace(/\s+/g, " ").toLowerCase();

const toIsoOrEmpty = (value: string | undefined) => (value && value.trim().length > 0 ? value : undefined);

const intervalsOverlapInclusive = (aStart: string, aEnd: string | null | undefined, bStart: string, bEnd: string | null | undefined) => {
  const aS = parseCalendarDate(aStart).getTime();
  const aE = aEnd ? parseCalendarDate(aEnd).getTime() : Number.POSITIVE_INFINITY;
  const bS = parseCalendarDate(bStart).getTime();
  const bE = bEnd ? parseCalendarDate(bEnd).getTime() : Number.POSITIVE_INFINITY;
  return aS <= bE && bS <= aE;
};

const createSchema = (existing: ContractEmployeeItem[]) =>
  z
    .object({
      employeeId: z.string().nonempty(),
      positionCode: z.string().nonempty(),
      positionName: z.string().nonempty(),
      workload: z
        .string()
        .nonempty()
        .refine((v) => isWholeWorkloadPercentInRange(v, 1, 100), Texts.enterWholeNumberRange1to100),
      startDate: z.string().nonempty(),
      endDate: z.string().optional(),
    })
    .superRefine((values, ctx) => {
      const employee = existing.find((e) => e.id === values.employeeId);
      if (!employee) return;

      const positionName = normalize(values.positionName);
      const startDate = values.startDate;
      const endDate = toIsoOrEmpty(values.endDate);

      const hasOverlap = employee.positions.some((p) => {
        const pName = normalize(p.position);
        if (pName !== positionName) return false;
        return intervalsOverlapInclusive(p.startDate, p.endDate, startDate, endDate);
      });

      if (hasOverlap) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          path: ["positionName"],
          message: Texts.employeeAlreadyHasPositionOverlap,
        });
      }
    });

interface AddEmployeeDialogProps {
  open: boolean;
  contractId: string;
  existingContractEmployees: ContractEmployeeItem[];
  onClose: () => void;
  onSaved: () => void;
}

export const AddEmployeeDialog = ({ open, contractId, existingContractEmployees, onClose, onSaved }: AddEmployeeDialogProps) => {
  const employeesFetcher = useFetcher<GetEmployeesResponse>();

  const resolver = useMemo(() => zodResolver(createSchema(existingContractEmployees)), [existingContractEmployees]);

  const form = useForm<AddEmployeeToContractFormValues>({
    resolver,
    mode: "onChange",
  });

  const startDate = form.watch("startDate");
  const endDate = form.watch("endDate");

  const employees: ComboBoxItem[] =
    employeesFetcher.data?.employees.map((e) => ({
      value: e.id,
      label: e.fullName,
    })) ?? [];

  useEffect(() => {
    if (!open) {
      return;
    }
    employeesFetcher.load(Routes.resourceEmployees());
  }, [open, employeesFetcher]);

  const handleOpenChange = (isOpen: boolean) => {
    if (!isOpen) {
      handleClose();
    }
  };

  const handleClose = () => {
    form.reset();
    onClose();
  };

  const handleSubmit = async (values: AddEmployeeToContractFormValues, signal: AbortSignal) => {
    const position = values.positionName.trim();
    const workload = workloadPercentToFraction(values.workload);
    const endDateIso = toIsoOrEmpty(values.endDate) ?? null;

    await addContractEmployee(
      contractId,
      {
        employeeId: values.employeeId,
        positionCode: values.positionCode.trim(),
        position,
        workload,
        startDate: values.startDate,
        endDate: endDateIso,
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
              name="employeeId"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{Texts.employees}</FormLabel>
                  <FormControl>
                    <ComboBox
                      value={field.value}
                      items={employees}
                      placeholder={Texts.employees}
                      loading={employeesFetcher.state !== "idle"}
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
                    <FormLabel>{Texts.positionCode}</FormLabel>
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
                    <FormLabel>{Texts.position}</FormLabel>
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
                  <FormLabel>{Texts.workload}</FormLabel>
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
