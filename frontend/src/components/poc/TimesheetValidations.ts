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
const MAX_NIGHT_WORK_HOURS = 8;
const MIN_BREAK_DURATION_HOURS = 0.5;
const MIN_REST_BETWEEN_SHIFTS_HOURS = 11;
/** Minimální odpracované hodiny před začátkem přestávky (interní pravidlo univerzity). */
const MIN_HOURS_WORKED_BEFORE_BREAK_ALLOWED = 4;
const HOURS_PRECISION = 2;

const toMinutes = (time: string): number => {
  if (!time) return 0;
  const [hours, minutes] = time.split(":").map(Number);
  return (hours || 0) * 60 + (minutes || 0);
};

const toHours = (minutes: number): number => {
  return Number((minutes / 60).toFixed(HOURS_PRECISION));
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
          message: "Odchod je dřív nebo ve stejný čas jako příchod.",
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
        message: "Chybí příchod.",
        field: "clockIn",
      });
    }

    // ERR-ATT-04: MissingClockOut (pouze pokud je vyplněn příchod)
    // Tato validace se spouští i pro víkendy a svátky, pokud je tam práce
    if (day.attendance.clockIn && !day.attendance.clockOut) {
      validations.push({
        code: "ERR-ATT-04",
        type: "error",
        message: "Chybí odchod.",
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
        message: "Odpracováno více než 12 hodin.",
        field: "clockOut",
      });
    }

    // ERR-ATT-10: NightWorkOverLimit (noční práce > 8h, po odečtení přestávky)
    if (day.attendance.clockIn && day.attendance.clockOut) {
      const nightHours = TimesheetLogic.calculateNightHours(day.attendance);
      if (nightHours > MAX_NIGHT_WORK_HOURS) {
        validations.push({
          code: "ERR-ATT-10",
          type: "error",
          message: "Noční práce přesahuje 8 hodin.",
          field: "clockOut",
        });
      }
    }

    // Validace přestávky (platí každý den, když je něco vyplněno)
    // ERR-ATT-08B: MissingBreakEnd (pokud je vyplněn začátek, musí být i konec)
    if (day.attendance.breakStart && !day.attendance.breakEnd) {
      validations.push({
        code: "ERR-ATT-08B",
        type: "error",
        message: "Chybí konec přestávky.",
        field: "breakEnd",
      });
    }

    // ERR-ATT-08C: MissingBreakStart (pokud je vyplněn konec, musí být i začátek)
    if (!day.attendance.breakStart && day.attendance.breakEnd) {
      validations.push({
        code: "ERR-ATT-08C",
        type: "error",
        message: "Chybí začátek přestávky.",
        field: "breakStart",
      });
    }

    // ERR-ATT-12: BreakWithoutClockInOut
    // Pokud je vyplněná přestávka, musí být vyplněn příchod i odchod.
    const hasAnyBreak = Boolean(day.attendance.breakStart || day.attendance.breakEnd);
    if (hasAnyBreak && !day.attendance.clockIn && !day.attendance.clockOut) {
      validations.push({
        code: "ERR-ATT-12",
        type: "error",
        message: "Aby šlo zadat přestávku, je potřeba vyplnit příchod i odchod.",
        field: "breakStart",
      });
    } else if (hasAnyBreak && !day.attendance.clockIn) {
      validations.push({
        code: "ERR-ATT-12A",
        type: "error",
        message: "Aby šlo zadat přestávku, doplňte prosím příchod",
        field: "breakStart",
      });
    } else if (hasAnyBreak && !day.attendance.clockOut) {
      validations.push({
        code: "ERR-ATT-12B",
        type: "error",
        message: "Aby šlo zadat přestávku, doplňte prosím odchod.",
        field: "breakStart",
      });
    }

    // ERR-ATT-11: Přestávka až po 4 hodinách od příchodu (nezávisle na limitu délky směny 12 h).
    if (day.attendance.breakStart && day.attendance.breakEnd && day.attendance.clockIn && day.attendance.clockOut) {
      const clockIn = toMinutes(day.attendance.clockIn);
      let breakStart = toMinutes(day.attendance.breakStart);
      if (breakStart < clockIn) {
        breakStart += 24 * 60;
      }
      const hoursWorkedBeforeBreak = toHours(breakStart - clockIn);
      if (hoursWorkedBeforeBreak + 1e-9 < MIN_HOURS_WORKED_BEFORE_BREAK_ALLOWED) {
        validations.push({
          code: "ERR-ATT-11",
          type: "error",
          message: "Přestávku lze čerpat až po 4 odpracovaných hodinách.",
          field: "breakStart",
        });
      }
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
            message: "Konec přestávky musí být po jejím začátku.",
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
            message: "Konec přestávky musí být po jejím začátku.",
            field: "breakEnd",
          });
        } else {
          const breakDuration = toHours(actualBreakEnd - breakStart);
          // Minimální délku přestávky (30 minut) vynucujeme jen tehdy, když je přestávka povinná
          // (tj. směna přesáhne 6 hodin).
          if (day.attendance.clockIn && day.attendance.clockOut) {
            const clockIn = toMinutes(day.attendance.clockIn);
            const clockOut = toMinutes(day.attendance.clockOut);
            const actualClockOut = clockOut < clockIn ? clockOut + 24 * 60 : clockOut;
            const shiftHours = toHours(actualClockOut - clockIn);
            const breakIsRequired = shiftHours > MAX_CONTINUOUS_WORK_BEFORE_BREAK_HOURS;

            if (breakIsRequired && breakDuration < MIN_BREAK_DURATION_HOURS) {
              validations.push({
                code: "ERR-ATT-06",
                type: "error",
                message: "Po 6 odpracovaných hodinách je nutná přestávka alespoň 30 minut.",
                field: "breakEnd",
              });
            }
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
            message: "Přestávka musí být mezi příchodem a odchodem.",
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
            message: "Přestávka musí být mezi příchodem a odchodem.",
            field: "breakEnd",
          });
        }
      }
    }

    // ERR-ATT-06/07: Break required (after 6h) + break too late
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

      // Pravidlo pauzy řešíme jen pro validní délku směny.
      // Nechceme ho ukazovat u zjevně nevalidních rozsahů (např. 08:00 -> 07:00 = 23h).
      if (workedHours > MAX_CONTINUOUS_WORK_BEFORE_BREAK_HOURS && workedHours <= MAX_WORK_SHIFT_HOURS) {
        const missingOrShortMsg = "Po 6 odpracovaných hodinách je nutná přestávka alespoň 30 minut.";
        const lateMsg = "Přestávka je příliš pozdě (musí být nejpozději po 6 hodinách práce).";

        // Missing any break boundary -> required break message
        if (!day.attendance.breakStart || !day.attendance.breakEnd) {
          validations.push({
            code: "ERR-ATT-06",
            type: "error",
            message: missingOrShortMsg,
            field: "breakStart",
          });
        } else {
          // Break duration (handles break across midnight)
          let breakStart = toMinutes(day.attendance.breakStart);
          let breakEnd = toMinutes(day.attendance.breakEnd);
          if (breakEnd < breakStart) {
            breakEnd += 24 * 60;
          }
          const breakDurationHours = toHours(breakEnd - breakStart);

          // Break must start within 6 hours from clock-in (handles shifts across midnight)
          if (breakStart < clockIn) {
            breakStart += 24 * 60;
          }
          const hoursWorkedBeforeBreak = toHours(breakStart - clockIn);

          // If break is too short -> required break message
          if (breakDurationHours < MIN_BREAK_DURATION_HOURS) {
            validations.push({
              code: "ERR-ATT-06",
              type: "error",
              message: missingOrShortMsg,
              field: "breakEnd",
            });
          } else if (hoursWorkedBeforeBreak > MAX_CONTINUOUS_WORK_BEFORE_BREAK_HOURS) {
            // If break exists and is long enough, but starts too late -> late-break message
            validations.push({
              code: "ERR-ATT-07",
              type: "error",
              message: lateMsg,
              field: "breakStart",
            });
          }
        }
      }
    }

    // WAR-ATT-04: NightShift (noční doba platí i pro víkendy a svátky)
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
          message: "Práce zasahuje do noční doby (22:00–05:59).",
          field: "clockIn",
        });
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
