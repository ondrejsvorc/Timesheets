import { zodResolver } from "@hookform/resolvers/zod";
import { useMemo } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { DialogCancelButton } from "@/components/shared/buttons/DialogButtons";
import { DatePicker } from "@/components/shared/inputs/DatePicker";
import { WorkloadPercentInput } from "@/components/shared/inputs/WorkloadPercentInput";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Form, FormControl, FormField, FormItem, FormLabel } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Texts } from "@/constants/texts";
import { parseCalendarDate } from "@/utils/calendarDate";
import { isWholeWorkloadPercentInRange, workloadFractionToPercent, workloadPercentToFraction } from "@/utils/workloadPercentForm";
import type { PositionItem, UpdateContractEmployeeRequest } from "./api";

type EditPositionFormValues = z.infer<ReturnType<typeof createSchema>>;

const toIsoOrEmpty = (value: string | undefined) => (value && value.trim().length > 0 ? value : undefined);

const createSchema = (position: PositionItem, projectStartDate: string, projectEndDate: string | null) =>
  z
    .object({
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

      const metadataChanged =
        values.positionCode.trim() !== position.positionCode || values.positionName.trim() !== position.position || workloadPercentToFraction(values.workload) !== position.workload;

      const endIso = toIsoOrEmpty(values.endDate) ?? projectEndDate ?? null;
      const endChanged = endIso !== position.endDate;
      const startChanged = values.startDate !== position.startDate;

      if (!metadataChanged && !endChanged && !startChanged) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          path: ["positionName"],
          message: Texts.updateImpactBlocked,
        });
      }

      if (metadataChanged && parseCalendarDate(values.startDate).getTime() <= parseCalendarDate(position.startDate).getTime()) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          path: ["startDate"],
          message: Texts.updatePositionStartAfterCurrent,
        });
      }
    });

interface EditContractEmployeePositionDialogProps {
  open: boolean;
  position: PositionItem;
  projectStartDate: string;
  projectEndDate: string | null;
  onClose: () => void;
  onContinue: (request: UpdateContractEmployeeRequest) => void;
}

export const EditContractEmployeePositionDialog = ({ open, position, projectStartDate, projectEndDate, onClose, onContinue }: EditContractEmployeePositionDialogProps) => {
  const resolver = useMemo(() => zodResolver(createSchema(position, projectStartDate, projectEndDate)), [position, projectEndDate, projectStartDate]);

  const form = useForm<EditPositionFormValues>({
    resolver,
    mode: "onChange",
    defaultValues: {
      positionCode: position.positionCode,
      positionName: position.position,
      workload: workloadFractionToPercent(position.workload),
      startDate: position.startDate,
      endDate: position.endDate ?? projectEndDate ?? undefined,
    },
  });

  const startDate = form.watch("startDate");
  const endDate = form.watch("endDate");

  const handleClose = () => {
    form.reset();
    onClose();
  };

  const handleSubmit = (values: EditPositionFormValues) => {
    onContinue({
      positionCode: values.positionCode.trim(),
      position: values.positionName.trim(),
      workload: workloadPercentToFraction(values.workload),
      startDate: values.startDate,
      endDate: toIsoOrEmpty(values.endDate) ?? projectEndDate ?? null,
    });
    form.reset();
  };

  return (
    <Dialog open={open} onOpenChange={(isOpen) => !isOpen && handleClose()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{Texts.editPositionTitle}</DialogTitle>
        </DialogHeader>

        <Form {...form}>
          <form className="space-y-4">
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
              <Button type="button" disabled={!form.formState.isValid} onClick={() => form.handleSubmit(handleSubmit)()}>
                {Texts.continueAction}
              </Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  );
};
