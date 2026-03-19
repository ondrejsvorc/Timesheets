/**
 * Knihovna pro výpočty v pracovním výkazu.
 * Všechny časové operace pracují s minutami pro zajištění přesnosti a snadné manipulace.
 */

const MINUTES_IN_HOUR = 60;
const MINUTES_IN_DAY = 1440;
const NIGHT_SHIFT_START_MINUTES = 1320; // 22:00
const NIGHT_SHIFT_END_MINUTES = 360;    // 06:00
const STANDARD_WORK_DAY_HOURS = 8;
const HOURS_PRECISION = 3;
const roundHours = (value: number) => Number(value.toFixed(HOURS_PRECISION));

/**
 * Převede časový řetězec ve formátu "HH:mm" na celkový počet minut od začátku dne.
 */
const convertTimeToMinutes = (time: string): number => {
  if (!time) return 0;
  const [hours, minutes] = time.split(":").map(Number);
  return (hours ?? 0) * MINUTES_IN_HOUR + (minutes ?? 0);
};

/**
 * Vypočítá délku trvání mezi dvěma časy. Podporuje přechod přes půlnoc.
 */
const calculateDurationMinutes = (startMinutes: number, endMinutes: number): number => {
  if (endMinutes < startMinutes) {
    return (endMinutes + MINUTES_IN_DAY) - startMinutes;
  }
  return endMinutes - startMinutes;
};

/**
 * Zjistí průnik dvou časových intervalů v rámci jednoho dne.
 */
const calculateIntervalOverlap = (range1Start: number, range1End: number, range2Start: number, range2End: number): number => {
  const latestStart = Math.max(range1Start, range2Start);
  const earliestEnd = Math.min(range1End, range2End);
  const overlap = earliestEnd - latestStart;
  return Math.max(0, overlap);
};

export const TimesheetLogic2 = {
  /**
   * Vypočítá čistý odpracovaný čas v desítkových hodinách.
   */
  calculateNetWorkedHours: (clockIn: string, clockOut: string, breakStart?: string, breakEnd?: string): number => {
    if (!clockIn || !clockOut) return 0;

    const startMinutes = convertTimeToMinutes(clockIn);
    const endMinutes = convertTimeToMinutes(clockOut);
    const grossDurationMinutes = calculateDurationMinutes(startMinutes, endMinutes);

    let breakDurationMinutes = 0;
    if (breakStart && breakEnd) {
      breakDurationMinutes = calculateDurationMinutes(convertTimeToMinutes(breakStart), convertTimeToMinutes(breakEnd));
    }

    const netMinutes = grossDurationMinutes - breakDurationMinutes;
    return roundHours(netMinutes / MINUTES_IN_HOUR);
  },

  /**
   * Vypočítá počet hodin odpracovaných v nočním tarifu (22:00 - 06:00).
   */
  calculateNightShiftHours: (clockIn: string, clockOut: string): number => {
    if (!clockIn || !clockOut) return 0;

    const startMinutes = convertTimeToMinutes(clockIn);
    const endMinutes = convertTimeToMinutes(clockOut);
    const isOvernightShift = endMinutes < startMinutes;

    const nightWindows = [
      { start: NIGHT_SHIFT_START_MINUTES, end: MINUTES_IN_DAY }, // 22:00 - 24:00
      { start: 0, end: NIGHT_SHIFT_END_MINUTES }                 // 00:00 - 06:00
    ];

    const totalNightMinutes = nightWindows.reduce((total, window) => {
      if (isOvernightShift) {
        // Pokud směna přechází půlnoc, kontrolujeme průnik v obou částech dne
        const overlapBeforeMidnight = calculateIntervalOverlap(startMinutes, MINUTES_IN_DAY, window.start, window.end);
        const overlapAfterMidnight = calculateIntervalOverlap(0, endMinutes, window.start, window.end);
        return total + overlapBeforeMidnight + overlapAfterMidnight;
      }
      return total + calculateIntervalOverlap(startMinutes, endMinutes, window.start, window.end);
    }, 0);

    return roundHours(totalNightMinutes / MINUTES_IN_HOUR);
  },

  /**
   * Vypočítá měsíční fond hodin pro konkrétní úvazek.
   */
  calculateMonthlyFund: (workingDaysInMonth: number, workload: number): number => {
    const totalFundHours = workingDaysInMonth * STANDARD_WORK_DAY_HOURS * workload;
    return roundHours(totalFundHours);
  },

  /**
   * Vypočítá rozdíl mezi odpracovanou dobou a alokovanými hodinami na projektech.
   */
  calculateDayBalance: (targetCapacity: number, coreHours: number | null, projectHours: Record<string, number>): number => {
    const totalAllocated = (coreHours ?? 0) + Object.values(projectHours).reduce((sum, h) => sum + h, 0);
    const deviation = totalAllocated - targetCapacity;
    return roundHours(deviation);
  }
} as const;