import { FormEvent, useState } from 'react'
import { managementApi } from './api'
import type { Customer, DashboardData, Driver, TransportOrder, Vehicle } from './types'

export type ModalKind = 'customer' | 'driver' | 'vehicle' | 'order' | 'shipment'

interface Props {
  kind: ModalKind
  entity?: Customer | Driver | Vehicle | TransportOrder
  data: DashboardData
  accessToken: string
  onClose: () => void
  onDone: (message: string) => void
}

const text = (form: FormData, name: string) => String(form.get(name) ?? '').trim()
const number = (form: FormData, name: string) => Number(form.get(name))
const optionalNumber = (form: FormData, name: string) => text(form, name) ? number(form, name) : null
const optionalText = (form: FormData, name: string) => text(form, name) || null
const utc = (form: FormData, name: string) => text(form, name) ? new Date(text(form, name)).toISOString() : null
const inputDate = (value?: string) => value ? value.slice(0, 10) : ''
const inputDateTime = (value?: string) => value ? new Date(value).toISOString().slice(0, 16) : ''

const labels: Record<ModalKind, string> = {
  customer: 'khách hàng', driver: 'tài xế', vehicle: 'phương tiện',
  order: 'đơn vận chuyển', shipment: 'chuyến xe',
}

export default function ManagementModal({ kind, entity, data, accessToken, onClose, onDone }: Props) {
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const form = new FormData(event.currentTarget)
    setSaving(true)
    setError('')
    try {
      if (kind === 'customer') {
        const current = entity as Customer | undefined
        const payload = {
          ...(current ? {} : { code: text(form, 'code') }), name: text(form, 'name'),
          phone: text(form, 'phone'), email: optionalText(form, 'email'),
          taxCode: optionalText(form, 'taxCode'), address: optionalText(form, 'address'),
        }
        await managementApi.saveCustomer(accessToken, current?.id, payload)
      }
      if (kind === 'driver') {
        const current = entity as Driver | undefined
        const payload = {
          ...(current ? {} : { employeeCode: text(form, 'employeeCode') }),
          fullName: text(form, 'fullName'), phone: text(form, 'phone'),
          licenseNumber: text(form, 'licenseNumber'), licenseClass: text(form, 'licenseClass'),
          licenseExpiryDate: text(form, 'licenseExpiryDate'),
        }
        await managementApi.saveDriver(accessToken, current?.id, payload)
        const newStatus = text(form, 'status')
        if (current && newStatus && newStatus !== current.status) await managementApi.setDriverStatus(accessToken, current.id, newStatus)
      }
      if (kind === 'vehicle') {
        const current = entity as Vehicle | undefined
        const payload = {
          ...(current ? {} : { licensePlate: text(form, 'licensePlate') }),
          vehicleType: text(form, 'vehicleType'), maxLoadKg: number(form, 'maxLoadKg'),
          cargoVolumeM3: optionalNumber(form, 'cargoVolumeM3'),
          registrationExpiryDate: optionalText(form, 'registrationExpiryDate'),
          insuranceExpiryDate: optionalText(form, 'insuranceExpiryDate'),
        }
        await managementApi.saveVehicle(accessToken, current?.id, payload)
        const newStatus = text(form, 'status')
        if (current && newStatus && newStatus !== current.status) await managementApi.setVehicleStatus(accessToken, current.id, newStatus)
      }
      if (kind === 'order') {
        const current = entity as TransportOrder | undefined
        const payload = {
          ...(current ? {} : { code: text(form, 'code'), customerId: text(form, 'customerId') }),
          pickupAddress: text(form, 'pickupAddress'), deliveryAddress: text(form, 'deliveryAddress'),
          goodsDescription: text(form, 'goodsDescription'), weightKg: number(form, 'weightKg'),
          packageCount: number(form, 'packageCount'), volumeM3: optionalNumber(form, 'volumeM3'),
          expectedPickupAtUtc: utc(form, 'expectedPickupAtUtc'), expectedDeliveryAtUtc: utc(form, 'expectedDeliveryAtUtc'),
          estimatedPrice: optionalNumber(form, 'estimatedPrice'), senderName: optionalText(form, 'senderName'),
          senderPhone: optionalText(form, 'senderPhone'), recipientName: optionalText(form, 'recipientName'),
          recipientPhone: optionalText(form, 'recipientPhone'), note: optionalText(form, 'note'),
        }
        await managementApi.saveOrder(accessToken, current?.id, payload)
      }
      if (kind === 'shipment') {
        const orderIds = form.getAll('orderIds').map(String)
        if (!orderIds.length) throw new Error('Vui lòng chọn ít nhất một đơn vận chuyển.')
        await managementApi.createShipment(accessToken, {
          code: text(form, 'code'), vehicleId: text(form, 'vehicleId'), driverId: text(form, 'driverId'),
          plannedDepartureAtUtc: utc(form, 'plannedDepartureAtUtc'), orderIds,
        })
      }
      onDone(`${entity ? 'Cập nhật' : 'Tạo'} ${labels[kind]} thành công.`)
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Không thể lưu dữ liệu.')
    } finally { setSaving(false) }
  }

  return <div className="modal-backdrop" role="presentation" onMouseDown={(e) => e.target === e.currentTarget && onClose()}>
    <section className={`management-modal modal-${kind}`} role="dialog" aria-modal="true" aria-labelledby="modal-title">
      <header><div><p>{entity ? 'CHỈNH SỬA THÔNG TIN' : 'TẠO MỚI'}</p><h2 id="modal-title">{entity ? 'Cập nhật' : 'Thêm'} {labels[kind]}</h2></div><button type="button" onClick={onClose} aria-label="Đóng">×</button></header>
      <form onSubmit={submit}>
        <div className="modal-body">
          {error && <div className="modal-error">{error}</div>}
          {kind === 'customer' && <CustomerFields value={entity as Customer | undefined}/>} 
          {kind === 'driver' && <DriverFields value={entity as Driver | undefined}/>} 
          {kind === 'vehicle' && <VehicleFields value={entity as Vehicle | undefined}/>} 
          {kind === 'order' && <OrderFields value={entity as TransportOrder | undefined} data={data}/>} 
          {kind === 'shipment' && <ShipmentFields data={data}/>} 
        </div>
        <footer><button type="button" className="cancel-button" onClick={onClose}>Hủy</button><button type="submit" className="save-button" disabled={saving}>{saving ? 'Đang lưu...' : entity ? 'Lưu thay đổi' : 'Tạo mới'}</button></footer>
      </form>
    </section>
  </div>
}

function Field({ label, name, defaultValue, type = 'text', required = false, placeholder, min, step }: { label: string; name: string; defaultValue?: string | number; type?: string; required?: boolean; placeholder?: string; min?: string; step?: string }) {
  return <label className="modal-field"><span>{label}{required && <b>*</b>}</span><input name={name} type={type} defaultValue={defaultValue ?? ''} required={required} placeholder={placeholder} min={min} step={step}/></label>
}

function CustomerFields({ value }: { value?: Customer }) {
  return <div className="form-grid">
    {!value && <Field label="Mã khách hàng" name="code" required placeholder="CUS-002"/>}
    <Field label="Tên khách hàng" name="name" defaultValue={value?.name} required/>
    <Field label="Số điện thoại" name="phone" defaultValue={value?.phone} required/>
    <Field label="Email" name="email" type="email" defaultValue={value?.email}/>
    <Field label="Mã số thuế" name="taxCode" defaultValue={value?.taxCode}/>
    <label className="modal-field full"><span>Địa chỉ</span><textarea name="address" defaultValue={value?.address ?? ''} rows={2}/></label>
  </div>
}

function DriverFields({ value }: { value?: Driver }) {
  return <div className="form-grid">
    {!value && <Field label="Mã nhân viên" name="employeeCode" required placeholder="DRV-002"/>}
    <Field label="Họ và tên" name="fullName" defaultValue={value?.fullName} required/>
    <Field label="Số điện thoại" name="phone" defaultValue={value?.phone} required/>
    <Field label="Số giấy phép" name="licenseNumber" defaultValue={value?.licenseNumber} required/>
    <Field label="Hạng bằng" name="licenseClass" defaultValue={value?.licenseClass} required/>
    <Field label="Ngày hết hạn" name="licenseExpiryDate" type="date" defaultValue={inputDate(value?.licenseExpiryDate)} required/>
    {value && <label className="modal-field"><span>Trạng thái</span><select name="status" defaultValue={value.status} disabled={value.status === 'Assigned'}>{value.status === 'Assigned' && <option value="Assigned">Đã phân công</option>}<option value="Available">Sẵn sàng</option><option value="OnLeave">Nghỉ phép</option><option value="Inactive">Ngừng hoạt động</option></select></label>}
  </div>
}

function VehicleFields({ value }: { value?: Vehicle }) {
  return <div className="form-grid">
    {!value && <Field label="Biển số" name="licensePlate" required placeholder="51C-123.45"/>}
    <Field label="Loại xe" name="vehicleType" defaultValue={value?.vehicleType} required/>
    <Field label="Tải trọng tối đa (kg)" name="maxLoadKg" type="number" min="1" step="0.01" defaultValue={value?.maxLoadKg} required/>
    <Field label="Thể tích (m³)" name="cargoVolumeM3" type="number" min="0.01" step="0.01" defaultValue={value?.cargoVolumeM3}/>
    <Field label="Hạn đăng kiểm" name="registrationExpiryDate" type="date" defaultValue={inputDate(value?.registrationExpiryDate)}/>
    <Field label="Hạn bảo hiểm" name="insuranceExpiryDate" type="date" defaultValue={inputDate(value?.insuranceExpiryDate)}/>
    {value && <label className="modal-field"><span>Trạng thái</span><select name="status" defaultValue={value.status} disabled={value.status === 'Assigned' || value.status === 'InTransit'}>{value.status === 'Assigned' && <option value="Assigned">Đã phân công</option>}{value.status === 'InTransit' && <option value="InTransit">Đang vận chuyển</option>}<option value="Available">Sẵn sàng</option><option value="Maintenance">Bảo trì</option><option value="Inactive">Ngừng hoạt động</option></select></label>}
  </div>
}

function OrderFields({ value, data }: { value?: TransportOrder; data: DashboardData }) {
  return <div className="form-grid">
    {!value && <><Field label="Mã đơn" name="code" required placeholder="ORD-002"/><label className="modal-field"><span>Khách hàng<b>*</b></span><select name="customerId" required defaultValue=""><option value="" disabled>Chọn khách hàng</option>{data.customers.filter(x => x.isActive).map(x => <option value={x.id} key={x.id}>{x.code} · {x.name}</option>)}</select></label></>}
    <label className="modal-field full"><span>Địa chỉ nhận hàng<b>*</b></span><textarea name="pickupAddress" defaultValue={value?.pickupAddress ?? ''} required rows={2}/></label>
    <label className="modal-field full"><span>Địa chỉ giao hàng<b>*</b></span><textarea name="deliveryAddress" defaultValue={value?.deliveryAddress ?? ''} required rows={2}/></label>
    <label className="modal-field full"><span>Mô tả hàng hóa<b>*</b></span><textarea name="goodsDescription" defaultValue={value?.goodsDescription ?? ''} required rows={2}/></label>
    <Field label="Khối lượng (kg)" name="weightKg" type="number" min="0.01" step="0.01" defaultValue={value?.weightKg} required/>
    <Field label="Số kiện" name="packageCount" type="number" min="1" step="1" defaultValue={value?.packageCount} required/>
    <Field label="Thể tích (m³)" name="volumeM3" type="number" min="0.01" step="0.01" defaultValue={value?.volumeM3}/>
    <Field label="Giá dự kiến" name="estimatedPrice" type="number" min="0" step="1000" defaultValue={value?.estimatedPrice}/>
    <Field label="Nhận hàng dự kiến" name="expectedPickupAtUtc" type="datetime-local" defaultValue={inputDateTime(value?.expectedPickupAtUtc)}/>
    <Field label="Giao hàng dự kiến" name="expectedDeliveryAtUtc" type="datetime-local" defaultValue={inputDateTime(value?.expectedDeliveryAtUtc)}/>
    <Field label="Người gửi" name="senderName" defaultValue={value?.senderName}/><Field label="SĐT người gửi" name="senderPhone" defaultValue={value?.senderPhone}/>
    <Field label="Người nhận" name="recipientName" defaultValue={value?.recipientName}/><Field label="SĐT người nhận" name="recipientPhone" defaultValue={value?.recipientPhone}/>
    <label className="modal-field full"><span>Ghi chú</span><textarea name="note" defaultValue={value?.note ?? ''} rows={2}/></label>
  </div>
}

function ShipmentFields({ data }: { data: DashboardData }) {
  const orders = data.orders.filter(x => x.status === 'WaitingForAssignment')
  return <div className="form-grid">
    <Field label="Mã chuyến" name="code" required placeholder="SHP-002"/>
    <Field label="Khởi hành dự kiến" name="plannedDepartureAtUtc" type="datetime-local" required/>
    <label className="modal-field"><span>Phương tiện<b>*</b></span><select name="vehicleId" required defaultValue=""><option value="" disabled>Chọn phương tiện</option>{data.vehicles.filter(x => x.status === 'Available').map(x => <option value={x.id} key={x.id}>{x.licensePlate} · {x.vehicleType} · {x.maxLoadKg}kg</option>)}</select></label>
    <label className="modal-field"><span>Tài xế<b>*</b></span><select name="driverId" required defaultValue=""><option value="" disabled>Chọn tài xế</option>{data.drivers.filter(x => x.status === 'Available').map(x => <option value={x.id} key={x.id}>{x.employeeCode} · {x.fullName}</option>)}</select></label>
    <fieldset className="order-picker full"><legend>Đơn chờ phân công</legend>{orders.length ? orders.map(x => <label key={x.id}><input type="checkbox" name="orderIds" value={x.id}/><span><b>{x.code}</b>{x.customerName}<small>{x.weightKg.toLocaleString('vi-VN')} kg · {x.pickupAddress} → {x.deliveryAddress}</small></span></label>) : <p>Không có đơn hàng đang chờ phân công.</p>}</fieldset>
  </div>
}

export function ConfirmDialog({ title, message, confirmLabel, danger, busy, onConfirm, onClose }: { title: string; message: string; confirmLabel: string; danger?: boolean; busy?: boolean; onConfirm: () => void; onClose: () => void }) {
  return <div className="modal-backdrop"><section className="confirm-dialog" role="alertdialog" aria-modal="true"><span className={danger ? 'danger' : ''}>!</span><h2>{title}</h2><p>{message}</p><div><button className="cancel-button" onClick={onClose}>Quay lại</button><button className={danger ? 'danger-button' : 'save-button'} disabled={busy} onClick={onConfirm}>{busy ? 'Đang xử lý...' : confirmLabel}</button></div></section></div>
}
