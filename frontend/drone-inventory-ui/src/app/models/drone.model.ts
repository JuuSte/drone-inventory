export type DroneStatus = 'Available' | 'Loaned';

export interface Drone {
  id: number;
  name: string;
  description: string;
  status: DroneStatus;
}

export interface CreateDroneRequest {
  name: string;
  description: string;
}
