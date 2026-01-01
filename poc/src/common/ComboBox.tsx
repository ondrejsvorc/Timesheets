import { Check, ChevronsUpDown, Loader2 } from "lucide-react";
import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Command, CommandEmpty, CommandGroup, CommandInput, CommandItem } from "@/components/ui/command";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { cn } from "@/lib/utils";

export interface ComboBoxItem {
  value: string;
  label: string;
}

interface ComboBoxProps {
  value?: string;
  items: ComboBoxItem[];
  placeholder: string;
  loading?: boolean;
  onChange: (value: string) => void;
}

export const ComboBox = ({ value, items, placeholder, loading, onChange }: ComboBoxProps) => {
  const [open, setOpen] = useState(false);
  const selected = items.find((i) => i.value === value);

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button variant="outline" role="combobox" className={cn("flex w-full items-center gap-2", !value && "text-muted-foreground")}>
          <span className="flex-1 truncate text-left">{selected?.label ?? placeholder}</span>
          <ChevronsUpDown className="size-4 opacity-50" />
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-full p-0">
        <Command>
          <CommandInput placeholder="Hledat…" />
          {loading ? (
            <div className="flex items-center justify-center py-6">
              <Loader2 className="size-5 animate-spin opacity-60 [animation-duration:0.5s]" />
            </div>
          ) : items.length === 0 ? (
            <CommandEmpty>Žádné položky</CommandEmpty>
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
  );
};
