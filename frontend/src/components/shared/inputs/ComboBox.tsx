import { Check, ChevronsUpDown, Loader2 } from "lucide-react";
import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Command, CommandEmpty, CommandGroup, CommandInput, CommandItem } from "@/components/ui/command";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { Texts } from "@/constants/texts";
import { cn } from "@/utils/cn";

export interface ComboBoxItem {
  value: string;
  label: string;
}

interface ComboBoxProps {
  value?: string;
  items: ComboBoxItem[];
  placeholder: string;
  loading?: boolean;
  disabled?: boolean;
  onChange: (value: string) => void;
}

export const ComboBox = ({ value, items, placeholder, loading, disabled, onChange }: ComboBoxProps) => {
  const [open, setOpen] = useState(false);
  const selected = items.find((i) => i.value === value);

  return (
    <div className="w-full">
      <Popover open={open} onOpenChange={setOpen}>
        <PopoverTrigger asChild>
          <Button type="button" variant="outline" role="combobox" disabled={disabled} className={cn("flex w-full items-center gap-2", !value && "text-muted-foreground")}>
            <span className="flex-1 truncate text-left">{selected?.label ?? placeholder}</span>
            <ChevronsUpDown className="size-4 opacity-50" />
          </Button>
        </PopoverTrigger>
        <PopoverContent className="w-[--radix-popover-trigger-width] p-0" align="start">
          <Command>
            <CommandInput placeholder={Texts.search} />
            {loading ? (
              <div className="flex items-center justify-center py-6">
                <Loader2 className="size-5 animate-spin opacity-60 [animation-duration:0.5s]" />
              </div>
            ) : items.length === 0 ? (
              <CommandEmpty>{Texts.noItems}</CommandEmpty>
            ) : (
              <CommandGroup>
                {items.map((item) => (
                  <CommandItem
                    key={item.value}
                    value={item.label}
                    onSelect={() => {
                      onChange(item.value);
                      setOpen(false);
                    }}
                    className="cursor-pointer"
                  >
                    <Check className={cn("mr-2 h-4 w-4", item.value === value ? "opacity-100" : "opacity-0")} />
                    {item.label}
                  </CommandItem>
                ))}
              </CommandGroup>
            )}
          </Command>
        </PopoverContent>
      </Popover>
    </div>
  );
};
