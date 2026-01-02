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
import { Textarea } from "@/components/ui/textarea";
import { Texts } from "@/constants/texts";
import { cn } from "@/utils/cn";
import { type CreateProjectContractRequest, createProjectContract } from "./api/createProjectContract";
import type { ProjectContractItem } from "./api/shared/projectContractItem";

const addContractSchema = z.object({
  name: z.string().nonempty(),
  contractId: z.string().nonempty(),
  startDate: z.string().nonempty(),
  endDate: z.string().nonempty(),
  description: z.string().optional(),
});

type AddContractFormValues = z.infer<typeof addContractSchema>;

interface AddContractDialogProps {
  projectId: string;
  open: boolean;
  onClose: () => void;
  onSaved: (contract: ProjectContractItem) => void;
}

export const AddContractDialog = ({ projectId, open, onClose, onSaved }: AddContractDialogProps) => {
  const form = useForm<AddContractFormValues>({
    resolver: zodResolver(addContractSchema),
    mode: "onChange",
  });

  const startDate = form.watch("startDate");
  const endDate = form.watch("endDate");

  const handleClose = () => {
    form.reset();
    onClose();
  };

  const handleSubmit = async (values: AddContractFormValues, signal: AbortSignal) => {
    const request: CreateProjectContractRequest = {
      name: values.name,
      registrationNumber: values.contractId,
      startDate: values.startDate,
      endDate: values.endDate,
      description: values.description,
    };

    const response = await createProjectContract(projectId, request, signal);
    onSaved(response.projectContract);
    form.reset();
  };

  return (
    <Dialog open={open} onOpenChange={(isOpen) => !isOpen && handleClose()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{Texts.newContract}</DialogTitle>
        </DialogHeader>

        <Form {...form}>
          <form className="space-y-4">
            <FormField
              control={form.control}
              name="name"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>
                    {Texts.contractName} <span className="text-destructive">*</span>
                  </FormLabel>
                  <FormControl>
                    <Input {...field} />
                  </FormControl>
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="contractId"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>
                    {Texts.contractId} <span className="text-destructive">*</span>
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

            <FormField
              control={form.control}
              name="description"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{Texts.contractDescription}</FormLabel>
                  <FormControl>
                    <Textarea className="resize-none" {...field} />
                  </FormControl>
                </FormItem>
              )}
            />

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
