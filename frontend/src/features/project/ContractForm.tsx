import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import z from "zod";
import { DialogCancelButton, DialogConfirmButton } from "@/components/shared/buttons/DialogButtons";
import { contractRegistrationNumberPattern, MaskedInput, maskContractRegistrationNumber } from "@/components/shared/inputs/MaskedInput";
import { DialogFooter } from "@/components/ui/dialog";
import { Form, FormControl, FormField, FormItem, FormLabel } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Texts } from "@/constants/texts";

export type ContractFormValues = z.infer<typeof contractSchema>;
export const contractSchema = z.object({
  name: z.string().nonempty(),
  contractId: z.string().regex(contractRegistrationNumberPattern),
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
    defaultValues: { contractId: "", name: "", ...initialValues },
  });

  return (
    <Form {...form}>
      <form className="space-y-4">
        <FormField
          control={form.control}
          name="contractId"
          render={({ field }) => (
            <FormItem>
              <FormLabel>
                {Texts.contractId} <span className="text-destructive">*</span>
              </FormLabel>
              <FormControl>
                <MaskedInput {...field} mask={maskContractRegistrationNumber} inputMode="numeric" placeholder="12345 12 1234 12" />
              </FormControl>
            </FormItem>
          )}
        />

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

        <DialogFooter>
          <DialogCancelButton onClick={onCancel} />
          <DialogConfirmButton disabled={!form.formState.isValid} onClick={(_, signal) => form.handleSubmit((values) => onSubmit(values, signal))()} />
        </DialogFooter>
      </form>
    </Form>
  );
};
