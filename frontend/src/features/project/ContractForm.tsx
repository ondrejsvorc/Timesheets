import { DialogCancelButton, DialogConfirmButton } from "@/components/shared/buttons/DialogButtons";
import { DialogFooter } from "@/components/ui/dialog";
import { Form, FormControl, FormField, FormItem, FormLabel } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Texts } from "@/constants/texts";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import z from "zod";

export type ContractFormValues = z.infer<typeof contractSchema>;
export const contractSchema = z.object({
  name: z.string().nonempty(),
  contractId: z.string().nonempty(),
});

interface ContractFormProps {
  initialValues?: Partial<ContractFormValues>;
  onSubmit: (values: ContractFormValues, signal: AbortSignal) => Promise<void>;
  onCancel: () => void;
}

export const ContractForm = ({ initialValues, onSubmit, onCancel }: ContractFormProps) => {
  const form = useForm<ContractFormValues>({
    resolver: zodResolver(contractSchema),
    mode: "onChange",
    defaultValues: initialValues,
  });

  return (
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
