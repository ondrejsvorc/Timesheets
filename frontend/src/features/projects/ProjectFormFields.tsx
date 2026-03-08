import { PopoverTrigger } from "@radix-ui/react-popover";
import { format } from "date-fns";
import { cs } from "date-fns/locale";
import { CalendarIcon } from "lucide-react";
import type { UseFormReturn } from "react-hook-form";
import { DialogCancelButton, DialogConfirmButton } from "@/components/shared/buttons/DialogButtons";
import { DialogFooter } from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Calendar } from "@/components/ui/calendar";
import { Form, FormControl, FormField, FormItem, FormLabel } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Popover, PopoverContent } from "@/components/ui/popover";
import { Texts } from "@/constants/texts";
import { cn } from "@/utils/cn";
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
                <Popover>
                  <PopoverTrigger asChild>
                    <FormControl>
                      <Button variant="outline" className={cn("w-full pl-3 text-left font-normal", !field.value && "text-muted-foreground")}>
                        {field.value ? format(field.value, "PPP", { locale: cs }) : undefined}
                        <CalendarIcon className="ml-auto h-4 w-4 opacity-50" />
                      </Button>
                    </FormControl>
                  </PopoverTrigger>
                  <PopoverContent className="w-auto p-0" align="start">
                    <Calendar
                      mode="single"
                      selected={field.value ? new Date(field.value) : undefined}
                      onSelect={(date) => {
                        if (!date) return;
                        const selected = field.value ? new Date(field.value) : null;
                        if (selected && date.toDateString() === selected.toDateString()) return;
                        field.onChange(date.toISOString());
                      }}
                      disabled={(date) => (endDate ? date >= new Date(endDate) : false)}
                      autoFocus
                    />
                  </PopoverContent>
                </Popover>
              </FormItem>
            )}
          />

          <FormField
            control={form.control}
            name="endDate"
            render={({ field }) => (
              <FormItem className="flex flex-col">
                <FormLabel>{Texts.endDate}</FormLabel>
                <Popover>
                  <PopoverTrigger asChild>
                    <FormControl>
                      <Button variant="outline" className={cn("w-full pl-3 text-left font-normal", !field.value && "text-muted-foreground")}>
                        {field.value ? format(field.value, "PPP", { locale: cs }) : undefined}
                        <CalendarIcon className="ml-auto h-4 w-4 opacity-50" />
                      </Button>
                    </FormControl>
                  </PopoverTrigger>
                  <PopoverContent className="w-auto p-0" align="start">
                    <Calendar
                      mode="single"
                      selected={field.value ? new Date(field.value) : undefined}
                      onSelect={(date) => {
                        if (!date) return;
                        const selected = field.value ? new Date(field.value) : null;
                        if (selected && date.toDateString() === selected.toDateString()) return;
                        field.onChange(date.toISOString());
                      }}
                      disabled={(date) => (startDate ? date <= new Date(startDate) : false)}
                      autoFocus
                    />
                  </PopoverContent>
                </Popover>
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
