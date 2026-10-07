import type { ApiResponse, ProblemDetails } from '../types'
import { authFetch } from '../auth'
import type { Customer, DashboardData, Driver, Shipment, TransportOrder, Vehicle } from './types'

async function get<T>(path: string, accessToken: string): Promise<T> {
  const response = await authFetch(path, {}, accessToken)

  if (!response.ok) {
    const problem = (await response.json().catch(() => ({}))) as ProblemDetails
    throw new Error(problem.detail || `Không thể tải dữ liệu (${response.status}).`)
  }

  const result = (await response.json()) as ApiResponse<T>
  return result.data
}

async function mutate<T = void>(path: string, accessToken: string, method: string, body?: unknown): Promise<T> {
  const response = await authFetch(path, {
    method,
    headers: body === undefined ? undefined : { 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body),
  }, accessToken)
  if (!response.ok) {
    const problem = (await response.json().catch(() => ({}))) as ProblemDetails
    throw new Error(problem.detail || `Thao tác thất bại (${response.status}).`)
  }
  if (response.status === 204) return undefined as T
  const result = (await response.json()) as ApiResponse<T>
  return result.data
}

export async function getDashboardData(accessToken: string): Promise<DashboardData> {
  const [customers, drivers, vehicles, orders, shipments] = await Promise.all([
    get<Customer[]>('/api/Customers', accessToken),
    get<Driver[]>('/api/Drivers', accessToken),
    get<Vehicle[]>('/api/Vehicles', accessToken),
    get<TransportOrder[]>('/api/TransportOrders', accessToken),
    get<Shipment[]>('/api/Shipments', accessToken),
  ])

  return { customers, drivers, vehicles, orders, shipments }
}

export const managementApi = {
  saveCustomer: (token: string, id: string | undefined, body: unknown) =>
    mutate(id ? `/api/Customers/${id}` : '/api/Customers', token, id ? 'PUT' : 'POST', body),
  setCustomerActive: (token: string, id: string, isActive: boolean) =>
    mutate(`/api/Customers/${id}/active`, token, 'PUT', { isActive }),
  saveDriver: (token: string, id: string | undefined, body: unknown) =>
    mutate(id ? `/api/Drivers/${id}` : '/api/Drivers', token, id ? 'PUT' : 'POST', body),
  setDriverStatus: (token: string, id: string, status: string) =>
    mutate(`/api/Drivers/${id}/status`, token, 'PUT', { status }),
  saveVehicle: (token: string, id: string | undefined, body: unknown) =>
    mutate(id ? `/api/Vehicles/${id}` : '/api/Vehicles', token, id ? 'PUT' : 'POST', body),
  setVehicleStatus: (token: string, id: string, status: string) =>
    mutate(`/api/Vehicles/${id}/status`, token, 'PUT', { status }),
  saveOrder: (token: string, id: string | undefined, body: unknown) =>
    mutate(id ? `/api/TransportOrders/${id}` : '/api/TransportOrders', token, id ? 'PUT' : 'POST', body),
  setOrderStatus: (token: string, id: string, status: string) =>
    mutate(`/api/TransportOrders/${id}/status`, token, 'PUT', { status }),
  createShipment: (token: string, body: unknown) => mutate('/api/Shipments', token, 'POST', body),
  shipmentAction: (token: string, id: string, action: 'start' | 'complete' | 'cancel') =>
    mutate(`/api/Shipments/${id}/${action}`, token, 'POST'),
}
