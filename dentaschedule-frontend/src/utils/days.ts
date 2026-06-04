export const DAY_NAMES = [
  'Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday',
] as const;

export function dayName(day: number): string {
  return DAY_NAMES[day] ?? `Day ${day}`;
}
