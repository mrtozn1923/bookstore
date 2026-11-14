// One row of GET /api/rum/p75 (route x metric x device)
export interface RumRow {
  route: string;
  metric: string;
  device: string;
  samples: number;
  p75: number;
  rating: string;
  alarm: boolean;
}
