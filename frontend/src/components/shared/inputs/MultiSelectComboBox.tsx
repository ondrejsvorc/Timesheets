import { Check, ChevronsUpDown, Loader2, X } from "lucide-react";
import { useState } from "react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Command, CommandEmpty, CommandGroup, CommandInput, CommandItem } from "@/components/ui/command";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { Texts } from "@/constants/texts";
import { cn } from "@/utils/cn";

export interface MultiSelectComboBoxItem {
  value: string;
  label?: string;
}

interface MultiSelectComboBoxProps {
  value: string[];
  items: MultiSelectComboBoxItem[];
  placeholder: string;
  loading?: boolean;
  maxVisibleItems?: number;
  onChange: (value: string[]) => void;
}

export const MultiSelectComboBox = ({ value = [], items, placeholder, loading, maxVisibleItems = 1, onChange }: MultiSelectComboBoxProps) => {
  const [open, setOpen] = useState(false);

  const toggleValue = (itemValue: string) => {
    const newValue = value.includes(itemValue) ? value.filter((v) => v !== itemValue) : [...value, itemValue];
    onChange(newValue);
  };

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <div className="relative w-[160px] h-[40px]">
          <Button
            variant="outline"
            role="combobox"
            aria-expanded={open}
            className={cn(
              "flex items-center justify-between gap-2 px-3 py-2 transition-all duration-200",
              "w-full h-full",
              value.length === 0 && "text-muted-foreground",
            )}
          >
            <div className="flex gap-1 flex-1 items-center overflow-hidden flex-nowrap">
              {value.length === 0 ? (
                <span className="truncate text-sm">{placeholder}</span>
              ) : (
                <>
                  {value.slice(0, maxVisibleItems).map((val) => (
                    <Badge key={val} variant="secondary" className="flex items-center gap-1 pr-1 font-normal shrink-0">
                      <span className="text-xs font-bold">{val}</span>
                      <button
                        type="button"
                        aria-label={`Odstranit ${val}`}
                        onClick={(e) => {
                          e.stopPropagation();
                          onChange(value.filter((v) => v !== val));
                        }}
                        className="rounded-full outline-none hover:bg-muted-foreground/20 focus-visible:ring-1 focus-visible:ring-ring transition-colors cursor-pointer inline-flex"
                      >
                        <X className="size-3" />
                      </button>
                    </Badge>
                  ))}
                  {value.length > maxVisibleItems && (
                    <span className="text-[10px] font-medium text-muted-foreground bg-muted px-1.5 py-0.5 rounded-sm">
                      +{value.length - maxVisibleItems}
                    </span>
                  )}
                </>
              )}
            </div>
            <ChevronsUpDown className="size-4 shrink-0 opacity-50" />
          </Button>
        </div>
      </PopoverTrigger>

      <PopoverContent className="w-[--radix-popover-trigger-width] p-0" align="start">
        <Command>
          <CommandInput placeholder={Texts.search} />
          {loading ? (
            <div className="flex items-center justify-center py-6">
              <Loader2 className="size-5 animate-spin opacity-60" />
            </div>
          ) : items.length === 0 ? (
            <CommandEmpty>{Texts.noItems}</CommandEmpty>
          ) : (
            <CommandGroup className="max-h-64 overflow-auto">
              {items.map((item) => {
                const isSelected = value.includes(item.value);
                return (
                  <CommandItem
                    key={item.value}
                    value={`${item.value} ${item.label}`}
                    onSelect={() => toggleValue(item.value)}
                    className="cursor-pointer"
                  >
                    <Check className={cn("mr-2 h-4 w-4", isSelected ? "opacity-100" : "opacity-0")} />
                    <span className="font-bold mr-2">{item.value}</span>
                    {item.label && <span className="text-muted-foreground truncate text-xs">{item.label}</span>}
                  </CommandItem>
                );
              })}
            </CommandGroup>
          )}
        </Command>
      </PopoverContent>
    </Popover>
  );
};
