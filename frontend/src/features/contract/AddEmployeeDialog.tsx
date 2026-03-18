import { DialogCancelButton, DialogConfirmButton } from "@/components/shared/buttons/DialogButtons";
import { ComboBox, type ComboBoxItem } from "@/components/shared/inputs/ComboBox";
import { DatePicker } from "@/components/shared/inputs/DatePicker";
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Form, FormControl, FormField, FormItem, FormLabel } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { zodResolver } from "@hookform/resolvers/zod";
import { parseISO } from "date-fns";
import { useEffect, useMemo } from "react";
import { useForm } from "react-hook-form";
import { useImmer } from "use-immer";
import { z } from "zod";
import { addContractEmployee } from "./api/addContractEmployee";
import type { EmployeeItem as ContractEmployeeItem } from "./api/getContractEmployees";
import { getEmployees } from "../employees/api/getEmployees";
import { Texts } from "@/constants/texts";

type AddEmployeeToContractFormValues = z.infer<ReturnType<typeof createSchema>>;

const normalize = (value: string) => value.trim().replace(/\s+/g, " ").toLowerCase();

const toIsoOrEmpty = (value: string | undefined) => (value && value.trim().length > 0 ? value : undefined);

const intervalsOverlapInclusive = (aStart: string, aEnd: string | null | undefined, bStart: string, bEnd: string | null | undefined) => {
  const aS = parseISO(aStart).getTime();
  const aE = aEnd ? parseISO(aEnd).getTime() : Number.POSITIVE_INFINITY;
  const bS = parseISO(bStart).getTime();
  const bE = bEnd ? parseISO(bEnd).getTime() : Number.POSITIVE_INFINITY;
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
        .refine((v) => {
          const n = Number(v.replace(",", "."));
          return Number.isFinite(n) && n > 0;
        }, "Úvazek musí být kladné číslo."),
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
          message: "Zaměstnanec už má tuto pozici na zakázce v překrývajícím se období.",
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
  const [employees, setEmployees] = useImmer<ComboBoxItem[]>([]);
  const [employeesLoading, setEmployeesLoading] = useImmer(false);

  const resolver = useMemo(() => zodResolver(createSchema(existingContractEmployees)), [existingContractEmployees]);

  const form = useForm<AddEmployeeToContractFormValues>({
    resolver,
    mode: "onChange",
  });

  const startDate = form.watch("startDate");
  const endDate = form.watch("endDate");

  useEffect(() => {
    if (!open) return;

    const loadEmployees = async () => {
      setEmployeesLoading(true);
      const response = await getEmployees().promise;
      setEmployees(
        response.employees.map((e) => ({
          value: e.id,
          label: `${e.fullName} · ${e.personalNumber}`,
        })),
      );
      setEmployeesLoading(false);
    };

    loadEmployees();
  }, [open, setEmployees, setEmployeesLoading]);

  const handleClose = () => {
    form.reset();
    onClose();
  };

  const handleSubmit = async (values: AddEmployeeToContractFormValues, signal: AbortSignal) => {
    const position = values.positionName.trim();
    const workload = Number(values.workload.replace(",", "."));
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
    <Dialog open={open} onOpenChange={(isOpen) => !isOpen && handleClose()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Přidat zaměstnanci pozici</DialogTitle>
        </DialogHeader>

        <Form {...form}>
          <form className="space-y-4">
            <FormField
              control={form.control}
              name="employeeId"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Zaměstnanec *</FormLabel>
                  <FormControl>
                    <ComboBox value={field.value} items={employees} placeholder="Vyberte zaměstnance" loading={employeesLoading} onChange={field.onChange} />
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
                    <Input {...field} inputMode="decimal" />
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
                          placeholder={Texts.noDate}
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
                disabled={!form.formState.isValid || employeesLoading}
                onClick={(_, signal) => form.handleSubmit((values) => handleSubmit(values, signal))()}
              />
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  );
};

