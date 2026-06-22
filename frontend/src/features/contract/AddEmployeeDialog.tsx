import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect, useMemo, useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { DialogCancelButton, DialogConfirmButton } from "@/components/shared/buttons/DialogButtons";
import { ComboBox, type ComboBoxItem } from "@/components/shared/inputs/ComboBox";
import { DatePicker } from "@/components/shared/inputs/DatePicker";
import { MaskedInput, maskPositionCode, positionCodePattern } from "@/components/shared/inputs/MaskedInput";
import { WorkloadPercentInput } from "@/components/shared/inputs/WorkloadPercentInput";
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Form, FormControl, FormField, FormItem, FormLabel } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Texts } from "@/constants/texts";
import { getEmployees } from "@/features/employees/api";
import { parseCalendarDate } from "@/utils/calendarDate";
import { isWorkloadPercentInRange, workloadPercentToFraction } from "@/utils/workloadPercentForm";
import { addContractEmployee, type EmployeeItem as ContractEmployeeItem, getAddContractEmployeeImpact } from "./api";

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

const createSchema = (existing: ContractEmployeeItem[], projectStartDate: string, projectEndDate: string | null) =>
  z
    .object({
      employeeId: z.string().nonempty(),
      positionCode: z.string().regex(positionCodePattern),
      positionName: z.string().nonempty(),
      workload: z
        .string()
        .nonempty()
        .refine((v) => isWorkloadPercentInRange(v, 1, 100), Texts.enterWorkloadRange1to100),
      startDate: z.string().nonempty(),
      endDate: z.string().optional(),
    })
    .superRefine((values, ctx) => {
      const start = parseCalendarDate(values.startDate);
      const end = values.endDate ? parseCalendarDate(values.endDate) : null;
      const projectStart = parseCalendarDate(projectStartDate);
      const projectEnd = projectEndDate ? parseCalendarDate(projectEndDate) : null;

      if (start < projectStart) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          path: ["startDate"],
          message: Texts.positionOutsideProjectRange,
        });
      }

      if (end && projectEnd && end > projectEnd) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          path: ["endDate"],
          message: Texts.positionOutsideProjectRange,
        });
      }

      const employee = existing.find((e) => e.id === values.employeeId);
      if (!employee) return;

      const positionName = normalize(values.positionName);
      const startDate = values.startDate;
      const endDate = toIsoOrEmpty(values.endDate) ?? projectEndDate ?? undefined;

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
  projectStartDate: string;
  projectEndDate: string | null;
  existingContractEmployees: ContractEmployeeItem[];
  onClose: () => void;
  onSaved: () => void;
}

export const AddEmployeeDialog = ({ open, contractId, projectStartDate, projectEndDate, existingContractEmployees, onClose, onSaved }: AddEmployeeDialogProps) => {
  const [employees, setEmployees] = useState<ComboBoxItem[]>([]);
  const [employeesLoading, setEmployeesLoading] = useState(false);
  const resolver = useMemo(() => zodResolver(createSchema(existingContractEmployees, projectStartDate, projectEndDate)), [existingContractEmployees, projectEndDate, projectStartDate]);

  const form = useForm<AddEmployeeToContractFormValues>({
    resolver,
    mode: "onChange",
  });

  const startDate = form.watch("startDate");
  const endDate = form.watch("endDate");

  useEffect(() => {
    if (!open) {
      return;
    }

    setEmployeesLoading(true);
    getEmployees()
      .then((response) =>
        setEmployees(
          response.employees.map((employee) => ({
            value: employee.id,
            label: employee.fullName,
            searchText: employee.personalNumber,
          })),
        ),
      )
      .finally(() => setEmployeesLoading(false));
  }, [open]);

  useEffect(() => {
    if (open && projectEndDate && !form.getValues("endDate")) {
      form.setValue("endDate", projectEndDate, { shouldValidate: true });
    }
  }, [form, open, projectEndDate]);

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
    const endDateIso = toIsoOrEmpty(values.endDate) ?? projectEndDate ?? null;

    const impact = await getAddContractEmployeeImpact(contractId, { employeeId: values.employeeId, startDate: values.startDate, endDate: endDateIso }, signal);

    if (!impact.canAdd) {
      throw new Error(impact.blockReason ?? Texts.addImpactBlocked);
    }

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

    if (signal.aborted) return;
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
          <form
            className="space-y-4"
            onSubmit={(e) => {
              // Prevent native form submission (would append query params to URL).
              e.preventDefault();
            }}
          >
            <FormField
              control={form.control}
              name="employeeId"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{Texts.employees}</FormLabel>
                  <FormControl>
                    <ComboBox value={field.value} items={employees} placeholder={Texts.employees} loading={employeesLoading} onChange={field.onChange} />
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
                      <MaskedInput {...field} mask={maskPositionCode} inputMode="numeric" placeholder="1.1.1.2.1.09" />
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
                          clearable={name !== "startDate" && !projectEndDate}
                          disabledDate={(date) => {
                            const beforeProject = date < parseCalendarDate(projectStartDate);
                            const afterProject = projectEndDate ? date > parseCalendarDate(projectEndDate) : false;
                            const outsideSelectedRange = name === "startDate" ? (endDate ? date >= parseCalendarDate(endDate) : false) : startDate ? date <= parseCalendarDate(startDate) : false;
                            return beforeProject || afterProject || outsideSelectedRange;
                          }}
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
              <DialogConfirmButton disabled={!form.formState.isValid} onClick={(_, signal) => form.handleSubmit((values) => handleSubmit(values, signal))()} />
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  );
};
