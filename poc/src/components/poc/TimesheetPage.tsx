import { useImmer } from "use-immer";
import { FullscreenWrapper } from "./FullscreenWrapper";
import type { Timesheet, TimesheetDay } from "./Timesheet";
import { TimesheetTable } from "./TimesheetTable";

const createMockTimesheet = (): Timesheet => {
  const year = 2026;
  const month = 1;

  const days: TimesheetDay[] = Array.from({ length: 31 }, (_, i) => {
    const dayNumber = i + 1;
    const dateObj = new Date(year, month - 1, dayNumber);
    const dayOfWeek = dateObj.getDay(); // 0=Ne, 6=So

    const isWeekend = dayOfWeek === 0 || dayOfWeek === 6;
    const isHoliday = dayNumber === 1; // 1.1. Nový rok

    // Simulujeme práci: jen v pracovní dny, které nejsou svátek
    const shouldHaveAttendance = !isWeekend && !isHoliday;

    return {
      date: `${dayNumber.toString().padStart(2, "0")}. 01. ${year}`,
      isWeekend,
      isHoliday,
      attendance: {
        clockIn: shouldHaveAttendance ? "08:00" : "",
        clockOut: shouldHaveAttendance ? "16:30" : "",
        breakStart: shouldHaveAttendance ? "12:00" : "",
        breakEnd: shouldHaveAttendance ? "12:30" : "",
        interruptions: "",
        nightHours: 0,
        schedules: [],
      },
      coreHours: 0,
      projectHours: {
        "p-a": 0,
        "p-b": 0,
      },
    };
  });

  return {
    year,
    month,
    totalWorkload: 1.0,
    core: { workload: 0.6 },
    projects: [
      { id: "p-a", registrationNumber: "22161", name: "Projekt A", workload: 0.3 },
      { id: "p-b", registrationNumber: "XXXXX", name: "Projekt B", workload: 0.1 },
    ],
    days,
  };
};

/**
 * Vytvoří testovací timesheet s fake daty pro testování všech validačních pravidel.
 * Každý den 1-14 obsahuje jiný typ chyby/varování.
 *
 * Testovací dny:
 * - Den 1: ERR-ATT-02 (Odchod před příchodem)
 * - Den 2: ERR-ATT-03 (Chybí příchod)
 * - Den 3: ERR-ATT-04 (Chybí odchod)
 * - Den 4: ERR-ATT-05 (Směna >12h)
 * - Den 5: ERR-ATT-06 (Chybí přestávka po 6h)
 * - Den 6: ERR-ATT-07 (Přestávka začíná po 6h)
 * - Den 7: ERR-ATT-08 (Přestávka <30min)
 * - Den 8: ERR-ATT-08A (Konec přestávky před začátkem)
 * - Den 9: ERR-ATT-09 (Přestávka mimo pracovní dobu)
 * - Den 10: Konec v 00:00 (pro test odpočinku)
 * - Den 11: ERR-COM-05 (Nedostatečný odpočinek mezi směnami)
 * - Den 12: WAR-ATT-04 (Noční práce)
 * - Den 13: WAR-COM-01 (Práce o víkendu)
 * - Den 14: WAR-COM-02 (Práce ve svátek)
 */
export const createTestTimesheet = (): Timesheet => {
  const year = 2026;
  const month = 1;

  const getDayInfo = (day: number) => {
    const date = new Date(year, month - 1, day);
    const dow = date.getDay();
    return {
      isWeekend: dow === 0 || dow === 6,
      isHoliday: day === 1,
    };
  };

  const baseDay = (day: number, attendance: Partial<TimesheetDay["attendance"]>): TimesheetDay => {
    const info = getDayInfo(day);
    return {
      date: `${day.toString().padStart(2, "0")}. 01. ${year}`,
      ...info,
      attendance: {
        clockIn: "",
        clockOut: "",
        breakStart: "",
        breakEnd: "",
        interruptions: "",
        nightHours: 0,
        schedules: [],
        ...attendance,
      },
      coreHours: 0,
      projectHours: {
        "p-a": 0,
        "p-b": 0,
      },
    };
  };

  const days: TimesheetDay[] = [
    // 1 – ERR-ATT-02: odchod před příchodem
    baseDay(1, { clockIn: "16:00", clockOut: "08:00" }),

    // 2 – ERR-ATT-03: chybí příchod
    baseDay(2, { clockOut: "16:30" }),

    // 3 – ERR-ATT-04: chybí odchod
    baseDay(3, { clockIn: "08:00" }),

    // 4 – ERR-ATT-05: směna > 12h
    baseDay(4, { clockIn: "08:00", clockOut: "21:30", breakStart: "12:00", breakEnd: "12:30" }),

    // 5 – ERR-ATT-06: žádná přestávka po 6h
    baseDay(5, { clockIn: "08:00", clockOut: "15:00" }),

    // 6 – ERR-ATT-07: přestávka pozdě
    baseDay(6, { clockIn: "08:00", clockOut: "16:00", breakStart: "15:00", breakEnd: "15:30" }),

    // 7 – ERR-ATT-08: přestávka < 30 min
    baseDay(7, { clockIn: "08:00", clockOut: "16:00", breakStart: "12:00", breakEnd: "12:15" }),

    // 8 – ERR-ATT-08A: konec přestávky před začátkem
    baseDay(8, { clockIn: "08:00", clockOut: "16:00", breakStart: "12:30", breakEnd: "12:00" }),

    // 9 – ERR-ATT-09: přestávka mimo pracovní dobu
    baseDay(9, { clockIn: "08:00", clockOut: "16:00", breakStart: "07:00", breakEnd: "07:30" }),

    // 10 – konec o půlnoci (příprava na test odpočinku)
    baseDay(10, { clockIn: "08:00", clockOut: "00:00", breakStart: "12:00", breakEnd: "12:30" }),

    // 11 – ERR-COM-05: nedostatečný odpočinek
    baseDay(11, { clockIn: "08:00", clockOut: "16:00", breakStart: "12:00", breakEnd: "12:30" }),

    // 12 – WAR-ATT-04: noční práce
    baseDay(12, { clockIn: "22:00", clockOut: "06:00", breakStart: "02:00", breakEnd: "02:30" }),
  ];

  // Zbytek měsíce – normální pracovní dny
  for (let d = 13; d <= 31; d++) {
    const info = getDayInfo(d);
    days.push(
      baseDay(
        d,
        info.isWeekend || info.isHoliday
          ? {}
          : {
              clockIn: "08:00",
              clockOut: "16:30",
              breakStart: "12:00",
              breakEnd: "12:30",
            },
      ),
    );
  }

  return {
    year,
    month,
    totalWorkload: 1.0,
    core: { workload: 0.6 },
    projects: [
      { id: "p-a", registrationNumber: "22161", name: "Projekt A", workload: 0.3 },
      { id: "p-b", registrationNumber: "XXXXX", name: "Projekt B", workload: 0.1 },
    ],
    days,
  };
};

/**
 * NÁVOD PRO MANUÁLNÍ TESTOVÁNÍ VALIDACÍ:
 *
 * 1. Pro automatické testování: Odkomentuj createTestTimesheet() a zakomentuj createMockTimesheet()
 *    - Dny 1-14 obsahují různé chyby/varování
 *
 * 2. Manuální testování jednotlivých pravidel:
 *
 * ERR-ATT-02 (Odchod před příchodem):
 *   - Zadej příchod: 16:00, odchod: 08:00
 *   - Očekávaná chyba: "Čas odchodu je dřívější nebo stejný jako příchod."
 *
 * ERR-ATT-03 (Chybí příchod):
 *   - Zadej pouze odchod: 16:30 (příchod nech prázdný)
 *   - Očekávaná chyba: "Není vyplněn čas příchodu."
 *
 * ERR-ATT-04 (Chybí odchod):
 *   - Zadej pouze příchod: 08:00 (odchod nech prázdný)
 *   - Očekávaná chyba: "Není vyplněn čas odchodu."
 *
 * ERR-ATT-05 (Směna >12h):
 *   - Zadej příchod: 08:00, odchod: 21:30 (13.5h)
 *   - Očekávaná chyba: "Odpracovaný čas za den překračuje 12 hodin."
 *
 * ERR-ATT-06 (Chybí přestávka po 6h):
 *   - Zadej příchod: 08:00, odchod: 15:00 (7h), přestávku nevyplňuj
 *   - Očekávaná chyba: "Chybí povinná přestávka po nejdéle 6 hodinách práce."
 *
 * ERR-ATT-07 (Přestávka začíná po 6h):
 *   - Zadej příchod: 08:00, odchod: 16:00, přestávka: 15:00-15:30 (začíná po 7h)
 *   - Očekávaná chyba: "Po 6 hodinách práce je povinné mít alespoň 30 minut přestávku."
 *
 * ERR-ATT-08 (Přestávka <30min):
 *   - Zadej příchod: 08:00, odchod: 16:00, přestávka: 12:00-12:15 (15min)
 *   - Očekávaná chyba: "Délka přestávky musí být alespoň 30 minut."
 *
 * ERR-ATT-08A (Konec přestávky před začátkem):
 *   - Zadej příchod: 08:00, odchod: 16:00, přestávka: 12:30-12:00
 *   - Očekávaná chyba: "Konec přestávky musí být později než začátek přestávky."
 *
 * ERR-ATT-09 (Přestávka mimo pracovní dobu):
 *   - Zadej příchod: 08:00, odchod: 16:00, přestávka: 07:00-07:30 (před příchodem)
 *   - NEBO přestávka: 17:00-17:30 (po odchodu)
 *   - Očekávaná chyba: "Přestávka musí být v rámci pracovní doby..."
 *
 * ERR-COM-05 (Nedostatečný odpočinek mezi směnami):
 *   - Den 1: příchod: 08:00, odchod: 00:00 (přes půlnoc)
 *   - Den 2: příchod: 08:00 (pouze 8h odpočinku)
 *   - Očekávaná chyba: "Mezi předchozím a aktuálním dnem není zajištěn minimální odpočinek 11 hodin..."
 *
 * WAR-ATT-04 (Noční práce):
 *   - Zadej příchod: 22:00 nebo odchod: 05:00
 *   - Očekávané varování: "Pracovní doba spadá do nočního intervalu (22:00 – 05:59)."
 *
 * WAR-COM-01 (Práce o víkendu):
 *   - Vyber sobotu nebo neděli a zadej příchod/odchod
 *   - Očekávané varování: "Práce evidovaná o víkendu..."
 *
 * WAR-COM-02 (Práce ve svátek):
 *   - Vyber svátek (např. 1.1.) a zadej příchod/odchod
 *   - Očekávané varování: "Práce evidovaná ve státní svátek..."
 */
export const TimesheetPage = () => {
  // Pro testování validací - načítáme testovací data s chybami
  const [timesheet, updateTimesheet] = useImmer<Timesheet>(createTestTimesheet());
  // const [timesheet, updateTimesheet] = useImmer<Timesheet>(createMockTimesheet());

  const handleUpdateDay = (date: string, recipe: (day: TimesheetDay) => void) => {
    updateTimesheet((draft) => {
      const day = draft.days.find((dayInstance) => dayInstance.date === date);
      if (day) recipe(day);
    });
  };

  return (
    <FullscreenWrapper>
      <TimesheetTable timesheet={timesheet} onUpdateDay={handleUpdateDay} />
    </FullscreenWrapper>
  );
};
