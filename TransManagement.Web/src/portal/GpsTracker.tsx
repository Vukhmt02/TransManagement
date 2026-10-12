import { useEffect, useRef, useState } from 'react'
import { driverPortalApi } from './api'
import './gps.css'

interface Props {
  accessToken: string
  shipmentId: string
  onLocationSent: () => void
}

export default function GpsTracker({ accessToken, shipmentId, onLocationSent }: Props) {
  const [tracking, setTracking] = useState(false)
  const [message, setMessage] = useState('')
  const watchId = useRef<number | null>(null)
  const lastSentAt = useRef(0)
  const sending = useRef(false)

  function stop() {
    if (watchId.current !== null) navigator.geolocation.clearWatch(watchId.current)
    watchId.current = null
    setTracking(false)
    setMessage('Đã dừng chia sẻ vị trí.')
  }

  function start() {
    if (!navigator.geolocation) {
      setMessage('Thiết bị hoặc trình duyệt không hỗ trợ GPS.')
      return
    }
    setMessage('Đang yêu cầu quyền truy cập vị trí...')
    watchId.current = navigator.geolocation.watchPosition(async position => {
      const now = Date.now()
      if (sending.current || now - lastSentAt.current < 10_000) return
      sending.current = true
      try {
        await driverPortalApi.location(accessToken, shipmentId, {
          latitude: position.coords.latitude,
          longitude: position.coords.longitude,
          speedKph: position.coords.speed == null ? null : Math.max(0, position.coords.speed * 3.6),
          accuracyMeters: position.coords.accuracy,
        })
        lastSentAt.current = now
        setTracking(true)
        setMessage(`Đã cập nhật lúc ${new Date().toLocaleTimeString('vi-VN')} · sai số khoảng ${Math.round(position.coords.accuracy)} m`)
        onLocationSent()
      } catch (error) {
        setMessage(error instanceof Error ? error.message : 'Không thể gửi vị trí lên hệ thống.')
      } finally { sending.current = false }
    }, error => {
      if (watchId.current !== null) navigator.geolocation.clearWatch(watchId.current)
      watchId.current = null
      setTracking(false)
      setMessage(error.code === error.PERMISSION_DENIED
        ? 'Quyền vị trí bị từ chối. Hãy cho phép Location trong trình duyệt.'
        : 'Không lấy được GPS. Hãy bật định vị và thử lại.')
    }, { enableHighAccuracy: true, maximumAge: 5_000, timeout: 20_000 })
    setTracking(true)
  }

  useEffect(() => () => {
    if (watchId.current !== null) navigator.geolocation.clearWatch(watchId.current)
  }, [])

  return <div className="gps-tracker">
    <button className={tracking ? 'gps-stop' : 'gps-start'} onClick={tracking ? stop : start}>
      {tracking ? 'Dừng chia sẻ GPS' : 'Bắt đầu chia sẻ GPS'}
    </button>
    {message && <small className={tracking ? 'gps-ok' : ''}><i/>{message}</small>}
  </div>
}
