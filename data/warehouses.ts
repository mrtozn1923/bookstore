// Bookstore.Inventory warehouses (11). Coordinates are city centres, rounded.
export interface Warehouse {
  code: string;
  name: string;
  latitude: number;
  longitude: number;
}

export const warehouses: Warehouse[] = [
  { code: "IST", name: "Istanbul warehouse", latitude: 41.01, longitude: 28.98 },
  { code: "ANK", name: "Ankara warehouse", latitude: 39.93, longitude: 32.86 },
  { code: "IZM", name: "Izmir warehouse", latitude: 38.42, longitude: 27.14 },
];
