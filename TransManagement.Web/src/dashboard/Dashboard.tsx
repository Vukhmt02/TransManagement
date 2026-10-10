import { useEffect, useMemo, useState } from 'react'
import type { UserProfile } from '../types'
import { getDashboardData, managementApi } from './api'
import type { Customer, DashboardData, DashboardSection, Driver, Shipment, TransportOrder, Vehicle } from './types'
import ManagementModal, { ConfirmDialog, type ModalKind } from './ManagementModal'
import { CustomerPortal, DriverPortal } from '../portal/Portals'
import './dashboard.css'
import './management-dashboard.css'
import './dashboard-large-type.css'

interface DashboardProps {
  accessToken: string
  profile: UserProfile | null
  onLogout: () => Promise<void>
}

const emptyData: DashboardData = { customers: [], drivers: [], vehicles: [], orders: [], shipments: [] }

const navigation: { id: DashboardSection; label: string; icon: string }[] = [
  { id: 'overview', label: 'Tổng quan', icon: 'grid' },
  { id: 'orders', label: 'Đơn vận chuyển', icon: 'box' },
  { id: 'shipments', label: 'Chuyến xe', icon: 'route' },
  { id: 'customers', label: 'Khách hàng', icon: 'users' },
  { id: 'drivers', label: 'Tài xế', icon: 'badge' },
  { id: 'vehicles', label: 'Phương tiện', icon: 'truck' },
]

const titles: Record<DashboardSection, { title: string; subtitle: string }> = {
  overview: { title: 'Tổng quan vận hành', subtitle: 'Theo dõi hoạt động vận tải theo thời gian thực' },
  orders: { title: 'Đơn vận chuyển', subtitle: 'Theo dõi trạng thái và tiến độ của tất cả đơn hàng' },
  shipments: { title: 'Chuyến xe', subtitle: 'Kiểm soát lịch trình, tài xế và phương tiện được phân công' },
  customers: { title: 'Khách hàng', subtitle: 'Danh sách khách hàng đang sử dụng dịch vụ' },
  drivers: { title: 'Tài xế', subtitle: 'Theo dõi trạng thái và giấy phép của đội ngũ tài xế' },
  vehicles: { title: 'Phương tiện', subtitle: 'Quản lý năng lực và tình trạng đội xe' },
}

const statusLabels: Record<string, string> = {
  Draft: 'Nháp', WaitingForAssignment: 'Chờ phân công', Assigned: 'Đã phân công',
  PickingUp: 'Đang lấy hàng', InTransit: 'Đang vận chuyển', Delivered: 'Đã giao',
  Completed: 'Hoàn thành', Cancelled: 'Đã hủy', DeliveryFailed: 'Giao thất bại',
  Planned: 'Đã lên kế hoạch', Started: 'Đang chạy', Available: 'Sẵn sàng',
  Active: 'Hoạt động', OnLeave: 'Nghỉ phép', Inactive: 'Ngừng hoạt động', Maintenance: 'Bảo trì',
}

function Icon({ name }: { name: string }) {
  const paths: Record<string, React.ReactNode> = {
    grid: <><rect x="3" y="3" width="7" height="7" rx="2"/><rect x="14" y="3" width="7" height="7" rx="2"/><rect x="3" y="14" width="7" height="7" rx="2"/><rect x="14" y="14" width="7" height="7" rx="2"/></>,
    box: <><path d="m4 7 8-4 8 4-8 4-8-4Z"/><path d="M4 7v10l8 4 8-4V7M12 11v10"/></>,
    route: <><circle cx="6" cy="18" r="3"/><circle cx="18" cy="6" r="3"/><path d="M8.5 16.5c5-2 2-6 7-8"/></>,
    users: <><circle cx="9" cy="8" r="3"/><path d="M3 19c.5-4 2.5-6 6-6s5.5 2 6 6"/><path d="M16 6a3 3 0 0 1 0 5.8M17 14c2.4.6 3.7 2.3 4 5"/></>,
    badge: <><circle cx="12" cy="8" r="4"/><path d="M5 21c.6-5 2.9-7.5 7-7.5s6.4 2.5 7 7.5"/><path d="m17 3 1 1 2-1v4"/></>,
    truck: <><path d="M3 6h11v11H3zM14 10h4l3 4v3h-7z"/><circle cx="7" cy="19" r="2"/><circle cx="18" cy="19" r="2"/></>,
    search: <><circle cx="10.5" cy="10.5" r="6.5"/><path d="m16 16 5 5"/></>,
    refresh: <><path d="M20 6v5h-5"/><path d="M18.5 9A8 8 0 1 0 20 15"/></>,
    menu: <><path d="M4 7h16M4 12h16M4 17h16"/></>,
    bell: <><path d="M18 9a6 6 0 0 0-12 0c0 7-3 7-3 7h18s-3 0-3-7"/><path d="M10 20h4"/></>,
    logout: <><path d="M10 4H5v16h5M14 8l4 4-4 4M8 12h10"/></>,
  }
  return <svg className="dash-icon" viewBox="0 0 24 24" aria-hidden="true">{paths[name]}</svg>
}

const status = (value: string) => <span className={`status status-${value.toLowerCase()}`}><i />{statusLabels[value] ?? value}</span>
const shortDate = (value?: string) => value ? new Intl.DateTimeFormat('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' }).format(new Date(value)) : '—'
const dateTime = (value?: string) => value ? new Intl.DateTimeFormat('vi-VN', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' }).format(new Date(value)) : '—'
const money = (value?: number) => value == null ? '—' : new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(value)

export default function Dashboard({ accessToken, profile, onLogout }: DashboardProps) {
  const [section, setSection] = useState<DashboardSection>('overview')
  const [data, setData] = useState<DashboardData>(emptyData)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [search, setSearch] = useState('')
  const [sidebarOpen, setSidebarOpen] = useState(false)
  const [modal, setModal] = useState<{ kind: ModalKind; entity?: Customer | Driver | Vehicle | TransportOrder } | null>(null)
  const [pending, setPending] = useState<{ type: 'customer-toggle' | 'order-cancel' | 'shipment-start' | 'shipment-complete' | 'shipment-cancel'; id: string; name: string; nextValue?: boolean } | null>(null)
  const [actionBusy, setActionBusy] = useState(false)
  const [toast, setToast] = useState('')
  const canManage = profile?.roles.some((role) => role === 'Admin' || role === 'Dispatcher') ?? false

  async function loadData() {
    if (!canManage) return
    setLoading(true)
    setError('')
    try { setData(await getDashboardData(accessToken)) }
    catch (reason) { setError(reason instanceof Error ? reason.message : 'Không thể tải dữ liệu dashboard.') }
    finally { setLoading(false) }
  }

  useEffect(() => { void loadData() }, [accessToken, canManage])
  useEffect(() => { setSearch('') }, [section])

  function completed(message: string) {
    setModal(null)
    setPending(null)
    setToast(message)
    window.setTimeout(() => setToast(''), 3500)
    void loadData()
  }

  async function executePending() {
    if (!pending) return
    setActionBusy(true)
    try {
      if (pending.type === 'customer-toggle') await managementApi.setCustomerActive(accessToken, pending.id, Boolean(pending.nextValue))
      if (pending.type === 'order-cancel') await managementApi.setOrderStatus(accessToken, pending.id, 'Cancelled')
      if (pending.type.startsWith('shipment-')) await managementApi.shipmentAction(accessToken, pending.id, pending.type.replace('shipment-', '') as 'start' | 'complete' | 'cancel')
      completed('Thao tác đã được cập nhật thành công.')
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Không thể thực hiện thao tác.')
      setPending(null)
    } finally { setActionBusy(false) }
  }

  if (!profile) return <div className="dashboard-boot"><span className="dash-spinner"/>Đang chuẩn bị bảng điều khiển...</div>

  if (!canManage && profile.roles.includes('Customer'))
    return <CustomerPortal accessToken={accessToken} profile={profile} onLogout={onLogout}/>

  if (!canManage && profile.roles.includes('Driver'))
    return <DriverPortal accessToken={accessToken} profile={profile} onLogout={onLogout}/>

  if (!canManage) {
    return <div className="restricted-page">
      <div className="restricted-brand"><span>TF</span> TRANSFLOW</div>
      <div className="restricted-card">
        <div className="restricted-icon">✓</div>
        <p>TÀI KHOẢN ĐÃ SẴN SÀNG</p>
        <h1>Xin chào, {profile.fullName}</h1>
        <span>Tài khoản của bạn có vai trò <b>{profile.roles.join(', ')}</b>. Dashboard điều hành dành cho Admin và Dispatcher; cổng khách hàng sẽ được phát triển ở bước tiếp theo.</span>
        <button onClick={() => void onLogout()}>Đăng xuất</button>
      </div>
    </div>
  }

  const changeSection = (next: DashboardSection) => { setSection(next); setSidebarOpen(false) }
  const current = titles[section]

  return <div className="dashboard-shell">
    <aside className={`dash-sidebar ${sidebarOpen ? 'open' : ''}`}>
      <div className="dash-logo"><span><Icon name="truck"/></span><strong>TRANS<b>FLOW</b></strong></div>
      <div className="workspace-card"><span><Icon name="truck"/></span><div><b>TransFlow</b><small>Vận hành thông minh</small></div><em>⌃</em></div>
      <div className="workspace-label">KHÔNG GIAN QUẢN LÝ</div>
      <nav>{navigation.map((item) => <button key={item.id} className={section === item.id ? 'active' : ''} onClick={() => changeSection(item.id)}><Icon name={item.icon}/><span>{item.label}</span>{item.id === 'orders' && data.orders.filter((x) => x.status === 'WaitingForAssignment').length > 0 && <em>{data.orders.filter((x) => x.status === 'WaitingForAssignment').length}</em>}</button>)}</nav>
      <div className="sidebar-account"><div className="avatar">{profile.fullName.charAt(0).toUpperCase()}</div><div><strong>{profile.fullName}</strong><span>{profile.roles[0]}</span></div><button title="Đăng xuất" onClick={() => void onLogout()}><Icon name="logout"/></button></div>
    </aside>
    {sidebarOpen && <button className="sidebar-scrim" aria-label="Đóng menu" onClick={() => setSidebarOpen(false)}/>} 

    <div className="dashboard-main">
      <header className="dash-header">
        <button className="menu-button" onClick={() => setSidebarOpen(true)}><Icon name="menu"/></button>
        <div className="dash-breadcrumb"><span><Icon name="truck"/></span><b>TransFlow</b><i>›</i><strong>{current.title}</strong></div>
        <div className="header-search"><Icon name="search"/><input value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Tìm phương tiện hoặc tài xế..."/></div>
        <div className="header-actions"><div className="header-profile"><div className="avatar">{profile.fullName.charAt(0).toUpperCase()}</div><span><strong>{profile.fullName}</strong><small>{profile.roles[0]}</small></span></div></div>
      </header>

      <main className="dashboard-content">
        <div className={`page-heading ${section === 'overview' ? 'overview-heading' : ''}`}><div><p>{new Intl.DateTimeFormat('vi-VN', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' }).format(new Date())}</p><h1>{section === 'overview' ? `Chào buổi sáng, ${profile.fullName.split(' ').slice(-1)[0]}!` : current.title}</h1><span>{section === 'overview' ? 'Tổng quan tình hình vận hành đội xe của bạn.' : current.subtitle}</span></div><button className="refresh-button" onClick={() => void loadData()} disabled={loading}><Icon name="refresh"/>{loading ? 'Đang tải...' : section === 'overview' ? '7 ngày gần nhất' : 'Làm mới'}</button></div>
        {error && <div className="dashboard-error"><b>Không thể cập nhật dữ liệu</b><span>{error}</span><button onClick={() => void loadData()}>Thử lại</button></div>}
        {loading ? <DashboardSkeleton/> : <SectionContent section={section} data={data} search={search} onEdit={(kind, entity) => setModal({ kind, entity })} onCreate={(kind) => setModal({ kind })} onConfirm={setPending}/>} 
      </main>
    </div>
    {modal && <ManagementModal {...modal} data={data} accessToken={accessToken} onClose={() => setModal(null)} onDone={completed}/>} 
    {pending && <ConfirmDialog title="Xác nhận thao tác" message={`Bạn có chắc muốn cập nhật “${pending.name}”? Thao tác sẽ thay đổi dữ liệu vận hành.`} confirmLabel="Xác nhận" danger={pending.type.includes('cancel') || (pending.type === 'customer-toggle' && !pending.nextValue)} busy={actionBusy} onClose={() => setPending(null)} onConfirm={() => void executePending()}/>} 
    {toast && <div className="dashboard-toast"><span>✓</span>{toast}</div>}
  </div>
}

function DashboardSkeleton() {
  return <div className="dash-skeleton"><div/><div/><div/><div/><section/><section/></div>
}

interface SectionProps {
  section: DashboardSection
  data: DashboardData
  search: string
  onEdit: (kind: ModalKind, entity: Customer | Driver | Vehicle | TransportOrder) => void
  onCreate: (kind: ModalKind) => void
  onConfirm: (action: { type: 'customer-toggle' | 'order-cancel' | 'shipment-start' | 'shipment-complete' | 'shipment-cancel'; id: string; name: string; nextValue?: boolean }) => void
}

function SectionContent({ section, data, search, onEdit, onCreate, onConfirm }: SectionProps) {
  const term = search.trim().toLocaleLowerCase('vi')
  const includes = (...values: (string | undefined)[]) => !term || values.some((value) => value?.toLocaleLowerCase('vi').includes(term))
  const orders = useMemo(() => data.orders.filter((x) => includes(x.code, x.customerName, x.pickupAddress, x.deliveryAddress, statusLabels[x.status])), [data.orders, term])
  const shipments = useMemo(() => data.shipments.filter((x) => includes(x.code, x.driverName, x.licensePlate, statusLabels[x.status])), [data.shipments, term])
  const customers = data.customers.filter((x) => includes(x.code, x.name, x.phone, x.email))
  const drivers = data.drivers.filter((x) => includes(x.employeeCode, x.fullName, x.phone, x.licenseNumber, statusLabels[x.status]))
  const vehicles = data.vehicles.filter((x) => includes(x.licensePlate, x.vehicleType, statusLabels[x.status]))

  if (section === 'overview') return <ManagementOverview data={data}/>
  if (section === 'orders') return <DataPanel count={orders.length} label="đơn vận chuyển" actionLabel="Tạo đơn" onAction={() => onCreate('order')}><table><thead><tr><th>Mã đơn</th><th>Khách hàng</th><th>Hành trình</th><th>Khối lượng</th><th>Giá dự kiến</th><th>Trạng thái</th><th/></tr></thead><tbody>{orders.map((x) => <tr key={x.id}><td><b className="code">{x.code}</b></td><td>{x.customerName}</td><td><div className="route-cell"><span>{x.pickupAddress}</span><i>→</i><span>{x.deliveryAddress}</span></div></td><td>{x.weightKg.toLocaleString('vi-VN')} kg</td><td>{money(x.estimatedPrice)}</td><td>{status(x.status)}</td><td><div className="row-actions">{['Draft','WaitingForAssignment'].includes(x.status) && <button onClick={() => onEdit('order', x)}>Sửa</button>}{['Draft','WaitingForAssignment'].includes(x.status) && <button className="danger" onClick={() => onConfirm({ type: 'order-cancel', id: x.id, name: x.code })}>Hủy</button>}</div></td></tr>)}</tbody></table></DataPanel>
  if (section === 'shipments') return <DataPanel count={shipments.length} label="chuyến xe" actionLabel="Tạo chuyến" onAction={() => onCreate('shipment')}><table><thead><tr><th>Mã chuyến</th><th>Phương tiện</th><th>Tài xế</th><th>Khởi hành</th><th>Số đơn</th><th>Trạng thái</th><th/></tr></thead><tbody>{shipments.map((x) => <tr key={x.id}><td><b className="code">{x.code}</b></td><td><b>{x.licensePlate}</b></td><td>{x.driverName}</td><td>{dateTime(x.plannedDepartureAtUtc)}</td><td>{x.orders.length} đơn</td><td>{status(x.status)}</td><td><ShipmentActions value={x} onConfirm={onConfirm}/></td></tr>)}</tbody></table></DataPanel>
  if (section === 'customers') return <DataPanel count={customers.length} label="khách hàng" actionLabel="Thêm khách hàng" onAction={() => onCreate('customer')}><table><thead><tr><th>Mã KH</th><th>Khách hàng</th><th>Liên hệ</th><th>Địa chỉ</th><th>Trạng thái</th><th/></tr></thead><tbody>{customers.map((x) => <tr key={x.id}><td><b className="code">{x.code}</b></td><td><b>{x.name}</b><small>{x.taxCode || 'Chưa có mã số thuế'}</small></td><td>{x.phone}<small>{x.email || '—'}</small></td><td className="truncate-cell">{x.address || '—'}</td><td>{status(x.isActive ? 'Active' : 'Inactive')}</td><td><div className="row-actions"><button onClick={() => onEdit('customer', x)}>Sửa</button><button className={x.isActive ? 'danger' : ''} onClick={() => onConfirm({ type: 'customer-toggle', id: x.id, name: x.name, nextValue: !x.isActive })}>{x.isActive ? 'Khóa' : 'Mở'}</button></div></td></tr>)}</tbody></table></DataPanel>
  if (section === 'drivers') return <DataPanel count={drivers.length} label="tài xế" actionLabel="Thêm tài xế" onAction={() => onCreate('driver')}><table><thead><tr><th>Mã NV</th><th>Tài xế</th><th>Điện thoại</th><th>Giấy phép</th><th>Hết hạn</th><th>Trạng thái</th><th/></tr></thead><tbody>{drivers.map((x) => <tr key={x.id}><td><b className="code">{x.employeeCode}</b></td><td><b>{x.fullName}</b></td><td>{x.phone}</td><td>{x.licenseNumber}<small>Hạng {x.licenseClass}</small></td><td>{shortDate(x.licenseExpiryDate)}</td><td>{status(x.status)}</td><td><div className="row-actions"><button onClick={() => onEdit('driver', x)}>Sửa</button></div></td></tr>)}</tbody></table></DataPanel>
  return <DataPanel count={vehicles.length} label="phương tiện" actionLabel="Thêm phương tiện" onAction={() => onCreate('vehicle')}><table><thead><tr><th>Biển số</th><th>Loại xe</th><th>Tải trọng</th><th>Thể tích</th><th>Đăng kiểm</th><th>Trạng thái</th><th/></tr></thead><tbody>{vehicles.map((x) => <tr key={x.id}><td><b className="plate">{x.licensePlate}</b></td><td>{x.vehicleType}</td><td>{x.maxLoadKg.toLocaleString('vi-VN')} kg</td><td>{x.cargoVolumeM3 ? `${x.cargoVolumeM3} m³` : '—'}</td><td>{shortDate(x.registrationExpiryDate)}</td><td>{status(x.status)}</td><td><div className="row-actions"><button onClick={() => onEdit('vehicle', x)}>Sửa</button></div></td></tr>)}</tbody></table></DataPanel>
}

function DataPanel({ count, label, children, actionLabel, onAction }: { count: number; label: string; children: React.ReactNode; actionLabel?: string; onAction?: () => void }) {
  return <section className="data-panel"><div className="panel-title"><div><h2>Danh sách</h2><span>{count} {label}</span></div>{actionLabel && <button className="panel-add" onClick={onAction}>+ {actionLabel}</button>}</div><div className="table-scroll">{count ? children : <div className="empty-state"><Icon name="box"/><b>Không tìm thấy dữ liệu</b><span>Thử thay đổi nội dung tìm kiếm hoặc tạo dữ liệu mới.</span>{actionLabel && <button className="panel-add" onClick={onAction}>+ {actionLabel}</button>}</div>}</div></section>
}

function ShipmentActions({ value, onConfirm }: { value: Shipment; onConfirm: SectionProps['onConfirm'] }) {
  return <div className="row-actions">
    {value.status === 'Assigned' && <button onClick={() => onConfirm({ type: 'shipment-start', id: value.id, name: value.code })}>Bắt đầu</button>}
    {value.status === 'Started' && <button onClick={() => onConfirm({ type: 'shipment-complete', id: value.id, name: value.code })}>Hoàn thành</button>}
    {['Planned','Assigned'].includes(value.status) && <button className="danger" onClick={() => onConfirm({ type: 'shipment-cancel', id: value.id, name: value.code })}>Hủy</button>}
  </div>
}

function ManagementOverview({ data }: { data: DashboardData }) {
  const activeTrips = data.shipments.filter(x => x.status === 'Started').length
  const maintenance = data.vehicles.filter(x => x.status === 'Maintenance').length
  const available = data.vehicles.filter(x => x.status === 'Available').length
  const completed = data.orders.filter(x => x.status === 'Completed').length
  const completionRate = data.orders.length ? Math.round(completed / data.orders.length * 100) : 0
  const topDrivers = data.drivers.slice(0, 5)
  const tracked = data.shipments.filter(x => x.latestLocation)

  return <div className="manage-overview">
    <section className="manage-stats">
      <ManageStat label="Tổng phương tiện" value={data.vehicles.length} icon="truck" tone="purple"/>
      <ManageStat label="Đang hoạt động" value={activeTrips} icon="route" tone="green"/>
      <ManageStat label="Đang bảo trì" value={maintenance} icon="truck" tone="red"/>
      <ManageStat label="Xe sẵn sàng" value={available} icon="truck" tone="orange"/>
      <ManageStat label="Tổng tài xế" value={data.drivers.length} icon="badge" tone="blue"/>
    </section>

    <section className="analytics-grid">
      <article className="analytics-card fleet-chart"><header><b>Trạng thái đội xe</b><button>Chi tiết</button></header><div className="fleet-donut" style={{ '--fleet': `${Math.max(8, data.vehicles.length ? available / data.vehicles.length * 360 : 0)}deg` } as React.CSSProperties}><span><small>Sẵn sàng</small><b>{data.vehicles.length ? Math.round(available / data.vehicles.length * 100) : 0}%</b></span></div><div className="fleet-legend"><span><i className="lg-a"/>Sẵn sàng ({available})</span><span><i className="lg-b"/>Đang chạy ({activeTrips})</span><span><i className="lg-c"/>Bảo trì ({maintenance})</span></div></article>
      <article className="analytics-card bar-chart"><header><b>Đơn hàng và chuyến xe</b><button>Chi tiết</button></header><div className="bars">{[35,62,51,43,69,78].map((height,index)=><div key={index}><i style={{height:`${height}%`}}/><em style={{height:`${Math.max(18,height-22)}%`}}/><small>T{index+1}</small></div>)}</div><footer><span><i/>Đơn hàng</span><span><i/>Chuyến xe</span></footer></article>
      <article className="analytics-card top-drivers"><header><b>Tài xế nổi bật</b><button>Chi tiết</button></header>{topDrivers.length ? topDrivers.map((driver,index)=><div className="driver-rank" key={driver.id}><span>{driver.fullName.charAt(0)}</span><div><b>{driver.fullName}</b><small>{driver.employeeCode} · Hạng {driver.licenseClass}</small></div><strong>{Math.max(80, 96-index*3)}</strong></div>) : <p className="manage-empty">Chưa có tài xế.</p>}</article>
    </section>

    <section className="analytics-card live-map"><header><b>Bản đồ hành trình trực tiếp</b><button>Xem chi tiết</button></header><div className="map-canvas"><div className="map-road road-one"/><div className="map-road road-two"/><div className="map-road road-three"/>{tracked.length ? tracked.slice(0,6).map((shipment,index)=><a key={shipment.id} className="map-vehicle" style={{left:`${16+index*14}%`,top:`${25+(index%3)*23}%`}} target="_blank" rel="noreferrer" href={`https://www.openstreetmap.org/?mlat=${shipment.latestLocation!.latitude}&mlon=${shipment.latestLocation!.longitude}`} title={`${shipment.code} - ${shipment.licensePlate}`}><Icon name="truck"/></a>) : <div className="map-placeholder"><Icon name="route"/><b>Chưa có vị trí GPS mới</b><span>Vị trí xe sẽ xuất hiện khi tài xế cập nhật hành trình.</span></div>}</div></section>
    <section className="manage-bottom"><article className="analytics-card"><header><b>Hiệu suất giao hàng</b><strong>{completionRate}%</strong></header><div className="progress-track"><i style={{width:`${completionRate}%`}}/></div><small>{completed}/{data.orders.length} đơn đã hoàn thành</small></article><article className="analytics-card"><header><b>Đơn chờ phân công</b><strong>{data.orders.filter(x=>x.status==='WaitingForAssignment').length}</strong></header><small>Cần điều phối phương tiện và tài xế</small></article></section>
  </div>
}

function ManageStat({ label, value, icon, tone }: { label: string; value: number; icon: string; tone: string }) {
  return <article className="manage-stat"><div><span>{label}</span><b>{value}</b></div><i className={tone}><Icon name={icon}/></i></article>
}

function Overview({ data }: { data: DashboardData }) {
  const waiting = data.orders.filter((x) => x.status === 'WaitingForAssignment').length
  const activeTrips = data.shipments.filter((x) => x.status === 'Started').length
  const availableVehicles = data.vehicles.filter((x) => x.status === 'Available').length
  const availableDrivers = data.drivers.filter((x) => x.status === 'Available').length
  const fleetRate = data.vehicles.length ? Math.round(availableVehicles / data.vehicles.length * 100) : 0
  const completed = data.orders.filter((x) => x.status === 'Completed').length
  const completionRate = data.orders.length ? Math.round(completed / data.orders.length * 100) : 0
  const recentOrders = data.orders.slice(0, 5)
  const currentTrips = data.shipments.filter((x) => x.status === 'Started' || x.status === 'Assigned').slice(0, 4)

  return <>
    <section className="stat-grid">
      <StatCard icon="box" color="amber" value={waiting} label="Đơn chờ phân công" note={`${data.orders.length} đơn trong hệ thống`}/>
      <StatCard icon="route" color="blue" value={activeTrips} label="Chuyến đang chạy" note={`${data.shipments.length} chuyến đã lập`}/>
      <StatCard icon="truck" color="teal" value={availableVehicles} label="Xe sẵn sàng" note={`${fleetRate}% tổng đội xe`}/>
      <StatCard icon="badge" color="violet" value={availableDrivers} label="Tài xế sẵn sàng" note={`${data.drivers.length} tài xế đang quản lý`}/>
    </section>
    <section className="overview-grid">
      <div className="dashboard-card activity-card"><div className="panel-title"><div><h2>Đơn vận chuyển gần đây</h2><span>Cập nhật theo dữ liệu mới nhất</span></div></div>{recentOrders.length ? <div className="recent-list">{recentOrders.map((x) => <div className="recent-row" key={x.id}><div className="order-symbol"><Icon name="box"/></div><div className="recent-main"><b>{x.code}</b><span>{x.customerName}</span></div><div className="recent-route"><span>{x.pickupAddress}</span><i>→</i><span>{x.deliveryAddress}</span></div>{status(x.status)}</div>)}</div> : <div className="empty-compact">Chưa có đơn vận chuyển.</div>}</div>
      <div className="dashboard-card performance-card"><div className="panel-title"><div><h2>Hiệu suất vận hành</h2><span>Tỷ lệ hoàn thành đơn hàng</span></div></div><div className="performance-body"><div className="donut" style={{ '--progress': `${completionRate * 3.6}deg` } as React.CSSProperties}><div><strong>{completionRate}%</strong><span>Hoàn thành</span></div></div><div className="legend"><p><i className="teal"/><span>Hoàn thành</span><b>{completed}</b></p><p><i className="amber"/><span>Đang xử lý</span><b>{data.orders.length - completed}</b></p><p><i className="gray"/><span>Tổng đơn</span><b>{data.orders.length}</b></p></div></div></div>
    </section>
    <section className="dashboard-card trip-card"><div className="panel-title"><div><h2>Chuyến xe cần theo dõi</h2><span>Các chuyến đã phân công hoặc đang vận chuyển</span></div></div>{currentTrips.length ? <div className="trip-grid">{currentTrips.map((x) => <div className="trip-item" key={x.id}><div className="trip-top"><b>{x.code}</b>{status(x.status)}</div><div className="trip-asset"><span className="mini-truck"><Icon name="truck"/></span><div><b>{x.licensePlate}</b><span>{x.driverName}</span></div></div><div className="trip-meta"><span>Khởi hành <b>{dateTime(x.plannedDepartureAtUtc)}</b></span><span>Đơn hàng <b>{x.orders.length}</b></span></div></div>)}</div> : <div className="empty-compact">Không có chuyến xe nào cần theo dõi.</div>}</section>
  </>
}

function StatCard({ icon, color, value, label, note }: { icon: string; color: string; value: number; label: string; note: string }) {
  return <div className="stat-card"><div className={`stat-icon ${color}`}><Icon name={icon}/></div><div className="stat-value"><strong>{value}</strong><span>{label}</span></div><small>{note}</small></div>
}
