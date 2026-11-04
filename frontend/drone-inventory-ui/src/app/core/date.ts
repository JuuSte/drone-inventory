/** Formats an ISO `yyyy-MM-dd` date as `dd.MM.yyyy` without going through `Date`. */
export function formatDate(isoDate: string): string {
  const [year, month, day] = isoDate.split('-');
  return year && month && day ? `${day}.${month}.${year}` : isoDate;
}
