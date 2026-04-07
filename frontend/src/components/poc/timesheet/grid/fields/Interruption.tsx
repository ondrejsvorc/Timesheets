import { MultiSelectComboBox, type MultiSelectComboBoxItem } from "@/components/shared/inputs/MultiSelectComboBox";
import type { EditableFieldProps } from "./FieldProps";

export const Interruption = ({ value, onChange }: EditableFieldProps<string>) => {
  const selected = value ? value.split(",").filter(Boolean) : [];

  return (
    <MultiSelectComboBox
      items={INTERRUPTION_OPTIONS}
      placeholder="Vyberte..."
      value={selected}
      onChange={(selectedArray) => onChange(selectedArray.join(","))}
    />
  );
};

const INTERRUPTION_OPTIONS: MultiSelectComboBoxItem[] = [
  { value: "D", label: "Dovolená" },
  { value: "JMV/HO", label: "Práce na dálku od 1.10.2023" },
  { value: "KAHO", label: "Karanténa -home office" },
  { value: "M", label: "Omluvená nepřítomnost - tvůrčí volno" },
  { value: "MD/OD", label: "Mateřská / Otcovská dovolená" },
  { value: "N", label: "Nemocenská" },
  { value: "NA", label: "Neomluvená absence" },
  { value: "NK", label: "Návštěva lékaře - krátkodobá" },
  { value: "NL", label: "Návštěva lékaře - celý den" },
  { value: "NP", label: "Pracovní úraz" },
  { value: "NV", label: "Náhradní volno" },
  { value: "O", label: "Ošetřovné" },
  { value: "OPN", label: "Osobní překážky" },
  { value: "PN", label: "Narození dítěte" },
  { value: "PO", label: "Odběr krve" },
  { value: "PS", label: "Svatba" },
  { value: "PU", label: "Úmrtí rod. příslušníka" },
  { value: "PVB", label: "Pracovní volno - branná povinnost" },
  { value: "PVM", label: "Pracovní volno - akce pro děti" },
  { value: "PZ", label: "Překážka na straně zaměstnavatele" },
  { value: "RD", label: "Rodičovská dovolená" },
  { value: "SCP", label: "Tuzemská služební cesta Projekt" },
  { value: "SCS", label: "Tuzemská služební cesta Stáž" },
  { value: "SCT", label: "Služební cesta" },
  { value: "SCZ", label: "Služební cesta zahraniční" },
  { value: "SCZE", label: "Zahraniční cesta Erasmus" },
  { value: "SCZP", label: "Zahraniční cesta Projekt" },
  { value: "SCZS", label: "Zahraniční cesta Stáž" },
  { value: "ST", label: "Studium s náhradou mzdy" },
  { value: "VN", label: "Neplacené volno" },
  { value: "VZ", label: "Nové zaměstnání" },
  { value: "Z", label: "Volno pro obecný zájem" },
  { value: "Zp", label: "Veřejná funkce - poslanec" },
  { value: "Zs", label: "Dlouhodobý pobyt v cizině" },
  { value: "Zv", label: "Zdravotní volno" },
];
