import { Suspense, useMemo } from "react";
import { Await, useAsyncValue, useLoaderData } from "react-router";
import { useForm } from "react-hook-form";
import { DialogCancelButton } from "@/components/shared/buttons/DialogButtons";
import { GenericSkeleton } from "@/components/shared/data/GenericSkeleton";
import { ComboBox, type ComboBoxItem } from "@/components/shared/inputs/ComboBox";
import { MultiSelectComboBox, type MultiSelectComboBoxItem } from "@/components/shared/inputs/MultiSelectComboBox";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Form, FormControl, FormField, FormItem, FormLabel } from "@/components/ui/form";
import { Textarea } from "@/components/ui/textarea";
import { Texts } from "@/constants/texts";
import type { ChangeTimesheetStatusOptions } from "./api/getChangeTimesheetStatusOptions";

type FormValues = {
  projectTimesheetIds: string[];
  statusId: string;
  comment: string;
};

interface ChangeTimesheetStatusDialogProps {
  open: boolean;
  onClose: () => void;
}

interface TimesheetPageLoaderData {
  changeTimesheetStatusOptionsPromise: Promise<ChangeTimesheetStatusOptions>;
}

const RequiredFormLabel = ({ children }: { children: string }) => (
  <FormLabel>
    {children} <span className="text-destructive">*</span>
  </FormLabel>
);

const ChangeTimesheetStatusForm = ({ onClose }: { onClose: () => void }) => {
  const options = useAsyncValue() as ChangeTimesheetStatusOptions;

  const projectTimesheetItems = useMemo<MultiSelectComboBoxItem[]>(
    () => options.projectTimesheets.map((projectTimesheet) => ({ value: projectTimesheet.id, label: projectTimesheet.label })),
    [options.projectTimesheets],
  );

  const statusItems = useMemo<ComboBoxItem[]>(
    () => options.statuses.map((status) => ({ value: status.id, label: status.name })),
    [options.statuses],
  );

  const form = useForm<FormValues>({
    defaultValues: {
      projectTimesheetIds: options.projectTimesheets.length > 0 ? [options.projectTimesheets[0].id] : [],
      statusId: options.currentStatusId,
      comment: "",
    },
  });

  const projectTimesheetIds = form.watch("projectTimesheetIds");
  const statusId = form.watch("statusId");
  const canConfirm = projectTimesheetIds.length > 0 && statusId.length > 0;

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
                  items={projectTimesheetItems}
                  placeholder={Texts.timesheetPicker}
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
          <Button type="button" disabled={!canConfirm} onClick={onClose}>
            {Texts.confirm}
          </Button>
        </DialogFooter>
      </form>
    </Form>
  );
};

export const ChangeTimesheetStatusDialog = ({ open, onClose }: ChangeTimesheetStatusDialogProps) => {
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
              <ChangeTimesheetStatusForm onClose={onClose} />
            </Await>
          </Suspense>
        ) : null}
      </DialogContent>
    </Dialog>
  );
};
