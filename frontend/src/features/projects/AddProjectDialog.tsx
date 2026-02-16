import { zodResolver } from "@hookform/resolvers/zod";
import { PopoverTrigger } from "@radix-ui/react-popover";
import { format } from "date-fns";
import { cs } from "date-fns/locale";
import { CalendarIcon } from "lucide-react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { DialogCancelButton, DialogConfirmButton } from "@/components/shared/buttons/DialogButtons";
import { Button } from "@/components/ui/button";
import { Calendar } from "@/components/ui/calendar";
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Form, FormControl, FormField, FormItem, FormLabel } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Popover, PopoverContent } from "@/components/ui/popover";
import { Texts } from "@/constants/texts";
import { cn } from "@/utils/cn";
import { type CreateProjectRequest, createProject } from "./api/createProject";
import type { ProjectItem } from "./api/shared/projectItem";

type AddProjectFormValues = z.infer<typeof addProjectSchema>;
const addProjectSchema = z.object({
  name: z.string().nonempty(),
  registrationNumber: z.string().nonempty(),
  startDate: z.string().nonempty(),
  endDate: z.string().nonempty(),
  description: z.string().optional(),
});

interface AddProjectDialogProps {
  open: boolean;
  onClose: () => void;
  onSaved: (project: ProjectItem) => void;
}

export const AddProjectDialog = ({ open, onClose, onSaved }: AddProjectDialogProps) => {
  const form = useForm<AddProjectFormValues>({ resolver: zodResolver(addProjectSchema), mode: "onChange" });
  const startDate = form.watch("startDate");
  const endDate = form.watch("endDate");

  const handleClose = () => {
    form.reset();
    onClose();
  };

  const handleSubmit = async (values: AddProjectFormValues, signal: AbortSignal) => {
    const request: CreateProjectRequest = {
      name: values.name,
      registrationNumber: values.registrationNumber,
      startDate: values.startDate,
      endDate: values.endDate,
      description: values.description,
    };
    const response = await createProject(request, signal);
    onSaved(response.project);
    form.reset();
  };

  return (
    <Dialog open={open} onOpenChange={(isOpen) => !isOpen && handleClose()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{Texts.newProject}</DialogTitle>
        </DialogHeader>

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
                            if (!date) {
                              return;
                            }
                            const selected = field.value ? new Date(field.value) : null;
                            if (selected && date.toDateString() === selected.toDateString()) {
                              return;
                            }
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
                    <FormLabel>
                      {Texts.endDate} <span className="text-destructive">*</span>
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
                            if (!date) {
                              return;
                            }
                            const selected = field.value ? new Date(field.value) : null;
                            if (selected && date.toDateString() === selected.toDateString()) {
                              return;
                            }
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
