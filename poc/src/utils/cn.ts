import { type ClassValue, clsx } from "clsx";
import { twMerge } from "tailwind-merge";

/** Merges Tailwind classes using clsx for conditional logic, then twMerge to resolve conflicts.
 * Used by Shadcn components to merge classes.
 * @param inputs - Tailwind classes to merge.
 * @returns Merged Tailwind classes.
 */
export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}
