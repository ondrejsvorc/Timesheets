import type { TimesheetDay } from "./Timesheet";
import { TimesheetLogic } from "./TimesheetLogic";

export type ValidationType = "error" | "warning";

export interface DayValidation {
  code: string;
  type: ValidationType;
  message: string;
  field?: string; // Název pole, které má chybu
}

const MAX_CONTINUOUS_WORK_BEFORE_BREAK_HOURS = 6;
const MAX_WORK_SHIFT_HOURS = 12;
const MIN_BREAK_DURATION_HOURS = 0.5;
const MIN_REST_BETWEEN_SHIFTS_HOURS = 11;

const toMinutes = (time: string): number => {
  if (!time) return 0;
  const [hours, minutes] = time.split(":").map(Number);
  return (hours || 0) * 60 + (minutes || 0);
};

const toHours = (minutes: number): number => {
  return Number((minutes / 60).toFixed(2));
};

export const TimesheetValidations = {
  /**
   * Validuje jeden den a vrátí seznam chyb/varování
   */
  validateDay: (day: TimesheetDay, previousDay?: TimesheetDay): DayValidation[] => {
    const validations: DayValidation[] = [];

    // ERR-ATT-02: ClockOutBeforeClockIn (ale ne pro noční směny)
    // Tato validace se spouští i pro víkendy a svátky, pokud je tam práce
    if (day.attendance.clockIn && day.attendance.clockOut) {
      const clockIn = toMinutes(day.attendance.clockIn);
      const clockOut = toMinutes(day.attendance.clockOut);

      // Pokud je odchod < příchod, zkontrolujeme, zda je to validní noční směna
      let isNightShift = false;
      if (clockOut < clockIn) {
        const actualClockOut = clockOut + 24 * 60;
        const workedMinutes = actualClockOut - clockIn;
        // Pokud je to rozumná noční směna (max 12h), je to OK
        if (workedMinutes <= 12 * 60) {
          isNightShift = true;
        }
      }

      if (clockOut <= clockIn && !isNightShift) {
        validations.push({
          code: "ERR-ATT-02",
          type: "error",
          message: "Čas odchodu je dřívější nebo stejný jako příchod.",
          field: "clockOut",
        });
      }
    }

    // ERR-ATT-03: MissingClockIn (pouze pokud je vyplněn odchod)
    // Tato validace se spouští i pro víkendy a svátky, pokud je tam práce
    if (!day.attendance.clockIn && day.attendance.clockOut) {
      validations.push({
        code: "ERR-ATT-03",
        type: "error",
        message: "Není vyplněn čas příchodu.",
        field: "clockIn",
      });
    }

    // ERR-ATT-04: MissingClockOut (pouze pokud je vyplněn příchod)
    // Tato validace se spouští i pro víkendy a svátky, pokud je tam práce
    if (day.attendance.clockIn && !day.attendance.clockOut) {
      validations.push({
        code: "ERR-ATT-04",
        type: "error",
        message: "Není vyplněn čas odchodu.",
        field: "clockOut",
      });
    }

    // ERR-ATT-05: TooLongWorkday
    // Tato validace se spouští i pro víkendy a svátky, pokud je tam práce
    const workedHours = TimesheetLogic.calculateWorkedHours(day.attendance);
    if (workedHours > MAX_WORK_SHIFT_HOURS) {
      validations.push({
        code: "ERR-ATT-05",
        type: "error",
        message: `Odpracovaný čas za den překračuje ${MAX_WORK_SHIFT_HOURS} hodin.`,
        field: "clockOut",
      });
    }

    // Validace přestávky (platí každý den, když je něco vyplněno)
    // ERR-ATT-08B: MissingBreakEnd (pokud je vyplněn začátek, musí být i konec)
    if (day.attendance.breakStart && !day.attendance.breakEnd) {
      validations.push({
        code: "ERR-ATT-08B",
        type: "error",
        message: "Není vyplněn konec přestávky.",
        field: "breakEnd",
      });
    }

    // ERR-ATT-08C: MissingBreakStart (pokud je vyplněn konec, musí být i začátek)
    if (!day.attendance.breakStart && day.attendance.breakEnd) {
      validations.push({
        code: "ERR-ATT-08C",
        type: "error",
        message: "Není vyplněn začátek přestávky.",
        field: "breakStart",
      });
    }

    // ERR-ATT-08: ShortBreak a BreakEndBeforeBreakStart
    if (day.attendance.breakStart && day.attendance.breakEnd) {
      const breakStart = toMinutes(day.attendance.breakStart);
      const breakEnd = toMinutes(day.attendance.breakEnd);

      let actualBreakEnd = breakEnd;
      const isBreakOverMidnight = breakEnd < breakStart;
      let isInvalidBreak = false;

      if (isBreakOverMidnight) {
        const breakDuration = breakEnd + 24 * 60 - breakStart;
        if (breakDuration > 12 * 60) {
          isInvalidBreak = true;
          validations.push({
            code: "ERR-ATT-08A",
            type: "error",
            message: "Konec přestávky musí být později než začátek přestávky.",
            field: "breakEnd",
          });
        } else {
          actualBreakEnd = breakEnd + 24 * 60;
        }
      }

      if (!isInvalidBreak) {
        if (actualBreakEnd <= breakStart) {
          validations.push({
            code: "ERR-ATT-08A",
            type: "error",
            message: "Konec přestávky musí být později než začátek přestávky.",
            field: "breakEnd",
          });
        } else {
          const breakDuration = toHours(actualBreakEnd - breakStart);
          if (breakDuration < MIN_BREAK_DURATION_HOURS) {
            validations.push({
              code: "ERR-ATT-08",
              type: "error",
              message: `Délka přestávky musí být alespoň ${MIN_BREAK_DURATION_HOURS * 60} minut.`,
              field: "breakEnd",
            });
          }
        }
      }
    }

    // ERR-ATT-09: BreakOutsideWorkInterval
    if (day.attendance.clockIn && day.attendance.clockOut && (day.attendance.breakStart || day.attendance.breakEnd)) {
      const clockIn = toMinutes(day.attendance.clockIn);
      const clockOut = toMinutes(day.attendance.clockOut);
      const isNightShift = clockOut < clockIn;

      if (day.attendance.breakStart) {
        const breakStart = toMinutes(day.attendance.breakStart);
        const breakStartValid = isNightShift
          ? (breakStart >= clockIn && breakStart <= 24 * 60 - 1) || (breakStart >= 0 && breakStart <= clockOut)
          : breakStart >= clockIn && breakStart <= clockOut;
        if (!breakStartValid) {
          validations.push({
            code: "ERR-ATT-09",
            type: "error",
            message: "Začátek přestávky musí být mezi příchodem a odchodem.",
            field: "breakStart",
          });
        }
      }

      if (day.attendance.breakEnd) {
        const breakEnd = toMinutes(day.attendance.breakEnd);
        const breakEndValid = isNightShift
          ? (breakEnd >= clockIn && breakEnd <= 24 * 60 - 1) || (breakEnd >= 0 && breakEnd <= clockOut)
          : breakEnd >= clockIn && breakEnd <= clockOut;
        if (!breakEndValid) {
          validations.push({
            code: "ERR-ATT-09",
            type: "error",
            message: "Konec přestávky musí být mezi příchodem a odchodem.",
            field: "breakEnd",
          });
        }
      }
    }

    if (!day.isWeekend && !day.isHoliday) {
      // ERR-ATT-06: MissingBreak
      if (day.attendance.clockIn && day.attendance.clockOut) {
        const clockIn = toMinutes(day.attendance.clockIn);
        const clockOut = toMinutes(day.attendance.clockOut);

        // Pokud je odchod < příchod, znamená to noční směnu přes půlnoc
        let actualClockOut = clockOut;
        if (clockOut < clockIn) {
          actualClockOut = clockOut + 24 * 60;
        }

        const workedMinutes = actualClockOut - clockIn;
        const workedHours = toHours(workedMinutes);

        if (workedHours > MAX_CONTINUOUS_WORK_BEFORE_BREAK_HOURS && !day.attendance.breakStart) {
          validations.push({
            code: "ERR-ATT-06",
            type: "error",
            message: `Povinná přestávka musí být nejpozději po ${MAX_CONTINUOUS_WORK_BEFORE_BREAK_HOURS} hodinách práce.`,
            field: "breakStart",
          });
        }
      }

      // ERR-ATT-07: LateBreak
      if (day.attendance.clockIn && day.attendance.breakStart) {
        const clockIn = toMinutes(day.attendance.clockIn);
        const breakStart = toMinutes(day.attendance.breakStart);

        // Pokud je přestávka před příchodem (noční směna přes půlnoc), musíme to správně vypočítat
        let hoursWorkedBeforeBreak: number;
        if (breakStart < clockIn) {
          // Přestávka je po půlnoci, příchod byl před půlnocí
          // Např. příchod 22:00, přestávka 04:00 = 6 hodin práce
          hoursWorkedBeforeBreak = toHours(breakStart + 24 * 60 - clockIn);
        } else {
          // Normální případ
          hoursWorkedBeforeBreak = toHours(breakStart - clockIn);
        }

        if (hoursWorkedBeforeBreak > MAX_CONTINUOUS_WORK_BEFORE_BREAK_HOURS) {
          validations.push({
            code: "ERR-ATT-07",
            type: "error",
            message: `Po ${MAX_CONTINUOUS_WORK_BEFORE_BREAK_HOURS} hodinách práce je povinné mít alespoň 30 minut přestávku.`,
            field: "breakStart",
          });
        }
      }

      // WAR-ATT-04: NightShift
      if (day.attendance.clockIn || day.attendance.clockOut) {
        const clockIn = day.attendance.clockIn ? toMinutes(day.attendance.clockIn) : null;
        const clockOut = day.attendance.clockOut ? toMinutes(day.attendance.clockOut) : null;

        const nightStart = 22 * 60; // 22:00
        const nightEnd = 5 * 60 + 59; // 05:59

        const clockInStartsAtNight = clockIn !== null && (clockIn >= nightStart || clockIn <= nightEnd);
        const clockOutEndsAtNight = clockOut !== null && (clockOut >= nightStart || clockOut <= nightEnd);

        if (clockInStartsAtNight || clockOutEndsAtNight) {
          validations.push({
            code: "WAR-ATT-04",
            type: "warning",
            message: "Pracovní doba spadá do nočního intervalu (22:00 – 05:59).",
            field: "clockIn",
          });
        }
      }
    }

    // WAR-COM-01: WeekendWork (pokud je vyplněn alespoň příchod nebo odchod – nezávisle na platnosti rozsahu)
    if (day.isWeekend && (day.attendance.clockIn || day.attendance.clockOut)) {
      validations.push({
        code: "WAR-COM-01",
        type: "warning",
        message: "Práce evidovaná o víkendu. Očekává se, že bude kompenzována v jiném pracovním dni.",
      });
    }

    // WAR-COM-02: HolidayWork (pokud je vyplněn alespoň příchod nebo odchod)
    if (day.isHoliday && (day.attendance.clockIn || day.attendance.clockOut)) {
      validations.push({
        code: "WAR-COM-02",
        type: "warning",
        message: "Práce evidovaná ve státní svátek. Očekává se, že bude kompenzována v jiném pracovním dni.",
      });
    }

    // ERR-COM-05: RestBetweenWorkDays (pokud máme předchozí den)
    if (previousDay && !day.isWeekend && !day.isHoliday && !previousDay.isWeekend && !previousDay.isHoliday) {
      if (previousDay.attendance.clockOut && day.attendance.clockIn) {
        // Zjednodušený výpočet - v reálném případě bychom potřebovali parsovat datum
        const prevOut = toMinutes(previousDay.attendance.clockOut);
        const currIn = toMinutes(day.attendance.clockIn);

        // Pokud je current start dříve než previous end, znamená to, že jsme přes půlnoc
        let restHours = toHours(currIn - prevOut);
        if (restHours < 0) {
          restHours = toHours(24 * 60 - prevOut + currIn);
        }

        if (restHours < MIN_REST_BETWEEN_SHIFTS_HOURS) {
          validations.push({
            code: "ERR-COM-05",
            type: "error",
            message: `Mezi předchozím a aktuálním dnem není zajištěn minimální odpočinek ${MIN_REST_BETWEEN_SHIFTS_HOURS} hodin.`,
            field: "clockIn",
          });
        }
      }
    }

    return validations;
  },
};
