export interface Warehouse {
  code: string;
  name: string;
  latitude: number;
  longitude: number;
}

// Great-circle distance (haversine); plenty for "which warehouse is closest".
export function distanceKm(
  a: { latitude: number; longitude: number },
  b: { latitude: number; longitude: number },
) {
  const rad = (deg: number) => (deg * Math.PI) / 180;
  const dLat = rad(b.latitude - a.latitude);
  const dLon = rad(b.longitude - a.longitude);
  const h =
    Math.sin(dLat / 2) ** 2 +
    Math.cos(rad(a.latitude)) * Math.cos(rad(b.latitude)) * Math.sin(dLon / 2) ** 2;
  return 2 * 6371 * Math.asin(Math.sqrt(h));
}

// Runs in the browser: the user's coordinates are not sent to the server.
export function nearest(
  warehouses: Warehouse[],
  position: { latitude: number; longitude: number },
) {
  const ranked = warehouses
    .map((warehouse) => ({ warehouse, km: Math.round(distanceKm(position, warehouse)) }))
    .sort((x, y) => x.km - y.km);
  return ranked[0];
}
