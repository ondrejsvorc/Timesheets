import type { UseFormReturn } from "react-hook-form";
import { DialogCancelButton, DialogConfirmButton } from "@/components/shared/buttons/DialogButtons";
import { DatePicker } from "@/components/shared/inputs/DatePicker";
import { DialogFooter } from "@/components/ui/dialog";
import { Form, FormControl, FormField, FormItem, FormLabel } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Texts } from "@/constants/texts";
import { z } from "zod";

export const projectFormSchema = z.object({
  name: z.string().min(1),
  registrationNumber: z.string().min(1),
  startDate: z.string().min(1),
  endDate: z.string().min(1).optional(),
});

export type ProjectFormValues = z.infer<typeof projectFormSchema>;

export const projectFormDefaultValues: ProjectFormValues = {
  name: "",
  registrationNumber: "",
  startDate: "",
  endDate: undefined,
};

interface ProjectFormFieldsProps {
  form: UseFormReturn<ProjectFormValues>;
  onSubmit: (values: ProjectFormValues, signal: AbortSignal) => Promise<void>;
  onCancel: () => void;
}

export const ProjectFormFields = ({ form, onSubmit, onCancel }: ProjectFormFieldsProps) => {
  const startDate = form.watch("startDate");
  const endDate = form.watch("endDate");

  return (
    <Form {...form}>
      <form className="space-y-4">
        <FormField
          control={form.control}
          name="name"
          render={({ field }) => (
            <FormItem>
              <FormLabel>
                {Texts.projectName} <span className="text-destructive">*</span>
              </FormLabel>
              <FormControl>
                <Input {...field} />
              </FormControl>
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="registrationNumber"
          render={({ field }) => (
            <FormItem>
              <FormLabel>
                {Texts.projectIdLabel} <span className="text-destructive">*</span>
              </FormLabel>
              <FormControl>
                <Input {...field} />
              </FormControl>
            </FormItem>
          )}
        />

        <div className="grid grid-cols-2 gap-4">
          <FormField
            control={form.control}
            name="startDate"
            render={({ field }) => (
              <FormItem className="flex flex-col">
                <FormLabel>
                  {Texts.startDate} <span className="text-destructive">*</span>
                </FormLabel>
                <FormControl>
                  <DatePicker
                    value={field.value}
                    placeholder={Texts.noDate}
                    clearable
                    disabledDate={(date) => (endDate ? date >= new Date(endDate) : false)}
                    onChange={(next) => field.onChange(next ?? "")}
                  />
                </FormControl>
              </FormItem>
            )}
          />

          <FormField
            control={form.control}
            name="endDate"
            render={({ field }) => (
              <FormItem className="flex flex-col">
                <FormLabel>{Texts.endDate}</FormLabel>
                <FormControl>
                  <DatePicker
                    value={field.value}
                    placeholder={Texts.noDate}
                    clearable
                    disabledDate={(date) => (startDate ? date <= new Date(startDate) : false)}
                    onChange={(next) => field.onChange(next)}
                  />
                </FormControl>
              </FormItem>
            )}
          />
        </div>

        <DialogFooter>
          <DialogCancelButton onClick={onCancel} />
          <DialogConfirmButton
            disabled={!form.formState.isValid}
            onClick={(_, signal) => form.handleSubmit((values) => onSubmit(values, signal))()}
          />
        </DialogFooter>
      </form>
    </Form>
  );
};
