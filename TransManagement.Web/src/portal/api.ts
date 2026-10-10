import { authFetch } from '../auth'
import type { ApiResponse, ProblemDetails } from '../types'
import type { Driver, Shipment, TransportOrder } from '../dashboard/types'

async function request<T>(token: string, path: string, init: RequestInit = {}): Promise<T> {
  const response = await authFetch(path, init, token)
  if (!response.ok) {
    const problem = (await response.json().catch(() => ({}))) as ProblemDetails
    throw new Error(problem.detail || `Thao tác thất bại (${response.status}).`)
  }
  if (response.status === 204) return undefined as T
  return ((await response.json()) as ApiResponse<T>).data
}

const json = (method: string, body?: unknown): RequestInit => ({
  method,
  headers: body === undefined ? undefined : { 'Content-Type': 'application/json' },
  body: body === undefined ? undefined : JSON.stringify(body),
})

export const customerPortalApi = {
  orders: (token: string) => request<TransportOrder[]>(token, '/api/TransportOrders/mine'),
  createOrder: (token: string, body: unknown) => request<TransportOrder>(token, '/api/TransportOrders/mine', json('POST', body)),
  cancelOrder: (token: string, id: string) => request<void>(token, `/api/TransportOrders/mine/${id}/cancel`, json('POST')),
}

export const driverPortalApi = {
  shipments: (token: string) => request<Shipment[]>(token, '/api/driver-portal/shipments'),
  shipmentAction: (token: string, id: string, action: 'start' | 'complete') => request<void>(token, `/api/driver-portal/shipments/${id}/${action}`, json('POST')),
  location: (token: string, id: string, body: unknown) => request(token, `/api/driver-portal/shipments/${id}/location`, json('POST', body)),
  arrive: (token: string, stopId: string, body: unknown) => request(token, `/api/driver-portal/stops/${stopId}/arrive`, json('POST', body)),
  completeStop: (token: string, stopId: string, body?: unknown) => request(token, `/api/driver-portal/stops/${stopId}/complete`, json('POST', body)),
  skipStop: (token: string, stopId: string) => request(token, `/api/driver-portal/stops/${stopId}/skip`, json('POST')),
}
