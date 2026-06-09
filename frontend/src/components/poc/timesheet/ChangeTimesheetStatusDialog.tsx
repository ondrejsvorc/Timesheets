import { Suspense, useMemo } from "react";
import { useForm } from "react-hook-form";
import { Await, useAsyncValue, useLoaderData, useRevalidator, useSearchParams } from "react-router";
import { DialogCancelButton, DialogConfirmButton } from "@/components/shared/buttons/DialogButtons";
import { GenericSkeleton } from "@/components/shared/data/GenericSkeleton";
import { ComboBox, type ComboBoxItem } from "@/components/shared/inputs/ComboBox";
import { MultiSelectComboBox, type MultiSelectComboBoxItem } from "@/components/shared/inputs/MultiSelectComboBox";
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Form, FormControl, FormField, FormItem, FormLabel } from "@/components/ui/form";
import { Textarea } from "@/components/ui/textarea";
import { Texts } from "@/constants/texts";
import type { ChangeTimesheetStatusOptions } from "./api/getChangeTimesheetStatusOptions";
import { updateCombinedTimesheetStatus } from "./api/updateCombinedTimesheetStatus";

type FormValues = {
  projectTimesheetIds: string[];
  statusId: string;
  comment: string;
};

interface ChangeTimesheetStatusDialogProps {
  open: boolean;
  onClose: () => void;
  onSuccess?: () => void;
}

interface TimesheetPageLoaderData {
  changeTimesheetStatusOptionsPromise: Promise<ChangeTimesheetStatusOptions>;
}

const RequiredFormLabel = ({ children }: { children: string }) => (
  <FormLabel>
    {children} <span className="text-destructive">*</span>
  </FormLabel>
);

const ChangeTimesheetStatusForm = ({ onClose, onSuccess }: { onClose: () => void; onSuccess?: () => void }) => {
  const options = useAsyncValue() as ChangeTimesheetStatusOptions;
  const [searchParams] = useSearchParams();
  const revalidator = useRevalidator();

  const employeeId = searchParams.get("employeeId");
  const year = Number(searchParams.get("year"));
  const month = Number(searchParams.get("month"));

  const timesheetItems = useMemo<MultiSelectComboBoxItem[]>(
    () => [
      { value: options.attendanceTimesheetId, label: Texts.attendance },
      ...options.projectTimesheets.map((projectTimesheet) => ({ value: projectTimesheet.id, label: projectTimesheet.label })),
    ],
    [options.attendanceTimesheetId, options.projectTimesheets],
  );

  const statusItems = useMemo<ComboBoxItem[]>(() => options.statuses.map((status) => ({ value: status.id, label: status.name })), [options.statuses]);

  const defaultTimesheetIds = useMemo(
    () => [options.attendanceTimesheetId, ...options.projectTimesheets.map((projectTimesheet) => projectTimesheet.id)],
    [options.attendanceTimesheetId, options.projectTimesheets],
  );

  const form = useForm<FormValues>({
    defaultValues: {
      projectTimesheetIds: defaultTimesheetIds,
      statusId: options.currentStatusId,
      comment: "",
    },
  });

  const projectTimesheetIds = form.watch("projectTimesheetIds");
  const statusId = form.watch("statusId");
  const canConfirm = projectTimesheetIds.length > 0 && statusId.length > 0 && employeeId && Number.isInteger(year) && Number.isInteger(month);

  const handleSubmit = async (_values: FormValues, signal: AbortSignal) => {
    if (!employeeId || !Number.isInteger(year) || !Number.isInteger(month)) {
      throw new Error("Missing timesheet context.");
    }

    await updateCombinedTimesheetStatus(
      {
        employeeId,
        year,
        month,
        statusId: _values.statusId,
        comment: _values.comment,
        timesheetIds: _values.projectTimesheetIds,
      },
      signal,
    );

    revalidator.revalidate();
    onSuccess?.();
    onClose();
  };

  return (
    <Form {...form}>
      <form className="space-y-4">
        <FormField
          control={form.control}
          name="projectTimesheetIds"
          render={({ field }) => (
            <FormItem>
              <RequiredFormLabel>{Texts.timesheetPicker}</RequiredFormLabel>
              <FormControl>
                <MultiSelectComboBox
                  value={field.value}
                  items={timesheetItems}
                  placeholder={Texts.timesheetPicker}
                  className="w-full"
                  onChange={field.onChange}
                />
              </FormControl>
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="statusId"
          render={({ field }) => (
            <FormItem>
              <RequiredFormLabel>{Texts.status}</RequiredFormLabel>
              <FormControl>
                <ComboBox value={field.value} items={statusItems} placeholder={Texts.status} onChange={field.onChange} />
              </FormControl>
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="comment"
          render={({ field }) => (
            <FormItem>
              <FormLabel>{Texts.comment}</FormLabel>
              <FormControl>
                <Textarea {...field} rows={4} />
              </FormControl>
            </FormItem>
          )}
        />

        <DialogFooter>
          <DialogCancelButton onClick={onClose} />
          <DialogConfirmButton disabled={!canConfirm} onClick={(_, signal) => form.handleSubmit((values) => handleSubmit(values, signal))()} />
        </DialogFooter>
      </form>
    </Form>
  );
};

export const ChangeTimesheetStatusDialog = ({ open, onClose, onSuccess }: ChangeTimesheetStatusDialogProps) => {
  const { changeTimesheetStatusOptionsPromise } = useLoaderData() as TimesheetPageLoaderData;

  return (
    <Dialog open={open} onOpenChange={(isOpen) => !isOpen && onClose()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{Texts.changeTimesheetStatus}</DialogTitle>
        </DialogHeader>

        {open ? (
          <Suspense fallback={<GenericSkeleton />}>
            <Await resolve={changeTimesheetStatusOptionsPromise}>
              <ChangeTimesheetStatusForm onClose={onClose} onSuccess={onSuccess} />
            </Await>
          </Suspense>
        ) : null}
      </DialogContent>
    </Dialog>
  );
};
