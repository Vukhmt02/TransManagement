import { useEffect, useMemo, useState } from 'react'
import { managementApi } from './api'
import type { Shipment, ShipmentLocation } from './types'
import './fleet-tracking-map.css'

export default function FleetTrackingMap({ accessToken, shipments }: { accessToken: string; shipments: Shipment[] }) {
  const active = useMemo(() => shipments.filter(x => x.status === 'Started'), [shipments])
  const [locations, setLocations] = useState<Record<string, ShipmentLocation>>({})
  const [selectedId, setSelectedId] = useState('')
  const [connection, setConnection] = useState<'loading'|'online'|'error'>('loading')

  useEffect(() => {
    setLocations(Object.fromEntries(shipments.filter(x => x.latestLocation).map(x => [x.id, x.latestLocation!])))
    setSelectedId(current => current && shipments.some(x => x.id === current) ? current : active[0]?.id ?? shipments.find(x => x.latestLocation)?.id ?? '')
  }, [shipments, active])

  useEffect(() => {
    if (!active.length) return
    let disposed = false
    const refresh = async () => {
      try {
        const responses = await Promise.all(active.map(async shipment => ({ shipment, items: await managementApi.shipmentLocations(accessToken, shipment.id) })))
        if (disposed) return
        setLocations(previous => {
          const next = { ...previous }
          for (const response of responses) if (response.items[0]) next[response.shipment.id] = response.items[0]
          return next
        })
        setConnection('online')
      } catch { if (!disposed) setConnection('error') }
    }
    void refresh()
    const timer = window.setInterval(() => void refresh(), 5_000)
    return () => { disposed = true; window.clearInterval(timer) }
  }, [accessToken, active])

  const selected = shipments.find(x => x.id === selectedId)
  const location = selectedId ? locations[selectedId] : undefined
  const mapUrl = location
    ? `https://www.openstreetmap.org/export/embed.html?bbox=${location.longitude-.025}%2C${location.latitude-.018}%2C${location.longitude+.025}%2C${location.latitude+.018}&layer=mapnik&marker=${location.latitude}%2C${location.longitude}`
    : ''

  return <section className="analytics-card real-live-map">
    <header><div><b>Bản đồ hành trình trực tiếp</b><small><i className={connection}/> {connection === 'online' ? 'Đang cập nhật mỗi 5 giây' : connection === 'error' ? 'Mất kết nối dữ liệu' : 'Đang kết nối...'}</small></div>{location && <a target="_blank" rel="noreferrer" href={`https://www.openstreetmap.org/?mlat=${location.latitude}&mlon=${location.longitude}#map=16/${location.latitude}/${location.longitude}`}>Mở bản đồ lớn ↗</a>}</header>
    <div className="fleet-map-layout">
      <aside>{shipments.filter(x => x.status === 'Started' || x.latestLocation).map(x => <button className={x.id === selectedId ? 'active' : ''} key={x.id} onClick={() => setSelectedId(x.id)}><span>🚚</span><div><b>{x.licensePlate}</b><small>{x.code} · {x.driverName}</small>{locations[x.id] && <em>{new Date(locations[x.id].recordedAtUtc).toLocaleTimeString('vi-VN')} · {locations[x.id].speedKph?.toFixed(0) ?? 0} km/h</em>}</div><i className={x.status === 'Started' ? 'moving' : ''}/></button>)}</aside>
      <div className="osm-frame">{mapUrl ? <><iframe title={`Vị trí ${selected?.licensePlate}`} src={mapUrl}/><div className="map-info"><b>{selected?.licensePlate}</b><span>{location!.latitude.toFixed(6)}, {location!.longitude.toFixed(6)}</span><span>Độ chính xác: {location!.accuracyMeters ? `${Math.round(location!.accuracyMeters)} m` : '—'}</span></div></> : <div className="map-no-location"><span>⌖</span><b>Chưa có dữ liệu GPS</b><small>Tài xế cần bắt đầu chuyến và bật chia sẻ vị trí.</small></div>}</div>
    </div>
  </section>
}
