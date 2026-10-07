export interface Customer {
  id: string
  code: string
  name: string
  phone: string
  email?: string
  taxCode?: string
  address?: string
  isActive: boolean
}

export interface Driver {
  id: string
  employeeCode: string
  fullName: string
  phone: string
  licenseNumber: string
  licenseClass: string
  licenseExpiryDate: string
  status: string
}

export interface Vehicle {
  id: string
  licensePlate: string
  vehicleType: string
  maxLoadKg: number
  cargoVolumeM3?: number
  registrationExpiryDate?: string
  insuranceExpiryDate?: string
  status: string
}

export interface TransportOrder {
  id: string
  code: string
  customerId: string
  customerName: string
  pickupAddress: string
  deliveryAddress: string
  goodsDescription: string
  weightKg: number
  volumeM3?: number
  packageCount: number
  expectedPickupAtUtc?: string
  expectedDeliveryAtUtc?: string
  estimatedPrice?: number
  senderName?: string
  senderPhone?: string
  recipientName?: string
  recipientPhone?: string
  note?: string
  status: string
}

export interface ShipmentOrder {
  orderId: string
  orderCode: string
  sequence: number
  pickupAddress: string
  deliveryAddress: string
  status: string
}

export interface Shipment {
  id: string
  code: string
  vehicleId: string
  licensePlate: string
  driverId: string
  driverName: string
  plannedDepartureAtUtc: string
  startedAtUtc?: string
  completedAtUtc?: string
  status: string
  orders: ShipmentOrder[]
}

export interface DashboardData {
  customers: Customer[]
  drivers: Driver[]
  vehicles: Vehicle[]
  orders: TransportOrder[]
  shipments: Shipment[]
}

export type DashboardSection = 'overview' | 'orders' | 'shipments' | 'customers' | 'drivers' | 'vehicles'
