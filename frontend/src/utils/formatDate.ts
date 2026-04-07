import { format, parseISO } from "date-fns";
import { cs } from "date-fns/locale";
import { Texts } from "@/constants/texts";

export const formatDate = (iso: string | null | undefined): string => {
  if (!iso) return Texts.dash;
  try {
    return format(parseISO(iso), "d. M. yyyy", { locale: cs });
  } catch {
    return Texts.dash;
  }
};
