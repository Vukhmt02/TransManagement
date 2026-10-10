import { FormEvent, useEffect, useState } from 'react'
import { getProfile, getStoredTokens, login, logout, register, requestRegistrationOtp } from './auth'
import type { AuthTokens, UserProfile } from './types'
import Dashboard from './dashboard/Dashboard'
import LandingPage from './landing/LandingPage'
import './otp.css'

const MailIcon = () => <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M4 6.75h16v10.5H4V6.75Z"/><path d="m4.5 7.25 7.5 6 7.5-6"/></svg>
const LockIcon = () => <svg viewBox="0 0 24 24" aria-hidden="true"><rect x="5" y="10" width="14" height="10" rx="2"/><path d="M8 10V7a4 4 0 0 1 8 0v3"/></svg>
const UserIcon = () => <svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="8" r="4"/><path d="M4.5 20c.7-4 3.2-6 7.5-6s6.8 2 7.5 6"/></svg>
const PhoneIcon = () => <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M7 3H4.5A1.5 1.5 0 0 0 3 4.5C3 13.6 10.4 21 19.5 21a1.5 1.5 0 0 0 1.5-1.5V17l-4-1-1.2 2a14 14 0 0 1-9.8-9.8L8 7 7 3Z"/></svg>
const EyeIcon = ({ hidden }: { hidden: boolean }) => <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M2.5 12s3.5-6 9.5-6 9.5 6 9.5 6-3.5 6-9.5 6-9.5-6-9.5-6Z"/><circle cx="12" cy="12" r="2.5"/>{hidden && <path d="m4 4 16 16"/>}</svg>
const TruckLogo = () => <div className="brand-mark" aria-hidden="true"><svg viewBox="0 0 32 32"><path d="M5 8h14v12H5zM19 12h4l4 4v4h-8z"/><circle cx="10" cy="22" r="2.5"/><circle cx="23" cy="22" r="2.5"/></svg></div>

const passwordRules = [
  { label: 'Ít nhất 8 ký tự', test: (value: string) => value.length >= 8 },
  { label: 'Chữ hoa và chữ thường', test: (value: string) => /[A-Z]/.test(value) && /[a-z]/.test(value) },
  { label: 'Ít nhất một chữ số', test: (value: string) => /\d/.test(value) },
  { label: 'Ít nhất một ký tự đặc biệt', test: (value: string) => /[^A-Za-z0-9]/.test(value) },
]

const isStrongPassword = (value: string) => passwordRules.every((rule) => rule.test(value))

function App() {
  const [mode, setMode] = useState<'login' | 'register'>('login')
  const [fullName, setFullName] = useState('')
  const [phone, setPhone] = useState('')
  const [email, setEmail] = useState(() => localStorage.getItem('transmanagement.email') ?? '')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [otp, setOtp] = useState('')
  const [otpSent, setOtpSent] = useState(false)
  const [rememberEmail, setRememberEmail] = useState(Boolean(localStorage.getItem('transmanagement.email')))
  const [showPassword, setShowPassword] = useState(false)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [tokens, setTokens] = useState<AuthTokens | null>(() => getStoredTokens())
  const [profile, setProfile] = useState<UserProfile | null>(null)
  const [showLanding, setShowLanding] = useState(true)

  useEffect(() => {
    if (!tokens) return
    getProfile(tokens.accessToken).then(setProfile).catch(() => {
      sessionStorage.removeItem('transmanagement.auth')
      setTokens(null)
    })
  }, [tokens])

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError('')
    if (!email.trim() || !password || (mode === 'register' && (!fullName.trim() || !phone.trim()))) {
      setError('Vui lòng nhập đầy đủ các thông tin bắt buộc.')
      return
    }
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim())) {
      setError('Địa chỉ email không đúng định dạng.')
      return
    }
    if (mode === 'register' && fullName.trim().length < 2) {
      setError('Họ và tên phải có ít nhất 2 ký tự.')
      return
    }
    if (mode === 'register' && password !== confirmPassword) {
      setError('Mật khẩu xác nhận không trùng khớp.')
      return
    }
    if (mode === 'register' && !isStrongPassword(password)) {
      setError('Mật khẩu chưa đáp ứng đầy đủ yêu cầu bảo mật.')
      return
    }
    setLoading(true)
    try {
      if (mode === 'register' && !otpSent) {
        await requestRegistrationOtp(email.trim())
        setOtpSent(true)
        setError('')
        return
      }
      if (mode === 'register' && !/^\d{6}$/.test(otp)) {
        setError('Vui lòng nhập đúng mã OTP gồm 6 chữ số được gửi qua email.')
        return
      }
      const newTokens = mode === 'login'
        ? await login(email.trim(), password)
        : await register(fullName.trim(), phone.trim(), email.trim(), password, otp)
      if (rememberEmail) localStorage.setItem('transmanagement.email', email.trim())
      else localStorage.removeItem('transmanagement.email')
      setTokens(newTokens)
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Không thể xử lý yêu cầu. Vui lòng thử lại.')
    } finally {
      setLoading(false)
    }
  }

  function switchMode(nextMode: 'login' | 'register') {
    setMode(nextMode)
    setError('')
    setPassword('')
    setConfirmPassword('')
    setOtp('')
    setOtpSent(false)
    setShowPassword(false)
  }

  async function handleLogout() {
    await logout(tokens)
    setTokens(null)
    setProfile(null)
    setPassword('')
  }

  if (tokens) {
    return <Dashboard accessToken={tokens.accessToken} profile={profile} onLogout={handleLogout}/>
  }

  if (showLanding) {
    return <LandingPage
      onLogin={() => { setMode('login'); setShowLanding(false); window.scrollTo(0, 0) }}
      onRegister={() => { setMode('register'); setShowLanding(false); window.scrollTo(0, 0) }}
    />
  }

  return (
    <main className="login-shell">
      <section className="story-panel">
        <div className="map-grid" aria-hidden="true" />
        <header className="story-header">
          <button className="brand brand-button" type="button" onClick={() => setShowLanding(true)} aria-label="TransFlow - Trang chủ"><TruckLogo /><span>TRANS<span>FLOW</span></span></button>
          <span className="portal-label"><i /> Cổng điều hành</span>
        </header>

        <div className="story-content">
          <p className="eyebrow">VẬN HÀNH THÔNG MINH · GIAO HÀNG ĐÚNG HẸN</p>
          <h1>Mọi hành trình.<br/><em>Một trung tâm.</em></h1>
          <p className="story-copy">Điều phối đội xe, tài xế và đơn hàng trên một nền tảng thống nhất — rõ ràng từ điểm nhận đến điểm giao.</p>
          <div className="route-card">
            <div className="route-topline"><span>Chuyến hàng hôm nay</span><strong>Đang vận hành</strong></div>
            <div className="route-line"><span className="route-dot active"/><span className="route-track"><i/></span><span className="truck-pin">➜</span><span className="route-track remaining"/><span className="route-dot"/></div>
            <div className="route-labels"><span><b>TP. Hồ Chí Minh</b><small>Kho trung tâm</small></span><span><b>Đà Nẵng</b><small>Điểm giao cuối</small></span></div>
          </div>
          <div className="metrics"><div><strong>98.7%</strong><span>Giao đúng hạn</span></div><div><strong>24/7</strong><span>Theo dõi vận hành</span></div><div><strong>1 nơi</strong><span>Quản lý tập trung</span></div></div>
        </div>
        <footer className="story-footer">© 2026 TransFlow · Transportation Management System</footer>
      </section>

      <section className="form-panel">
        <div className="mobile-brand"><TruckLogo/><span>TRANS<b>FLOW</b></span></div>
        {tokens ? (
          <div className="signed-in-card">
            <div className="success-icon">{profile ? profile.fullName.charAt(0).toUpperCase() : '✓'}</div>
            <p className="eyebrow dark">PHIÊN ĐĂNG NHẬP ĐANG HOẠT ĐỘNG</p>
            <h2>{profile ? `Xin chào, ${profile.fullName}` : 'Đang tải tài khoản...'}</h2>
            {profile && <><p className="profile-email">{profile.email}</p><div className="role-list">{profile.roles.map((role) => <span key={role}>{role}</span>)}</div></>}
            <div className="signed-actions">
              <button className="primary-button" type="button" onClick={handleLogout}>Đăng xuất an toàn <span>→</span></button>
            </div>
          </div>
        ) : (
          <div className="form-wrap">
            <div className="auth-tabs" role="tablist" aria-label="Chọn hình thức xác thực">
              <button type="button" role="tab" aria-selected={mode === 'login'} className={mode === 'login' ? 'active' : ''} onClick={() => switchMode('login')}>Đăng nhập</button>
              <button type="button" role="tab" aria-selected={mode === 'register'} className={mode === 'register' ? 'active' : ''} onClick={() => switchMode('register')}>Đăng ký</button>
            </div>
            <div className="form-heading">
              <span className="welcome-chip">{mode === 'login' ? 'Chào mừng trở lại' : 'Bắt đầu hành trình'}</span>
              <h2>{mode === 'login' ? 'Đăng nhập hệ thống' : 'Tạo tài khoản mới'}</h2>
              <p>{mode === 'login' ? 'Sử dụng tài khoản được cấp để tiếp tục vào cổng quản lý vận tải.' : 'Đăng ký tài khoản khách hàng để sử dụng các dịch vụ vận chuyển.'}</p>
            </div>
            {error && <div className="error-banner" role="alert"><span>!</span>{error}</div>}
            <form onSubmit={handleSubmit} noValidate>
              {mode === 'register' && <>
                <label htmlFor="fullName">Họ và tên</label>
                <div className="input-wrap"><span className="input-icon"><UserIcon/></span><input id="fullName" name="fullName" type="text" autoComplete="name" maxLength={200} placeholder="Nguyễn Văn An" value={fullName} onChange={(event) => setFullName(event.target.value)} disabled={loading}/></div>
                <label className="field-spaced" htmlFor="phone">Số điện thoại</label>
                <div className="input-wrap"><span className="input-icon"><PhoneIcon/></span><input id="phone" name="phone" type="tel" autoComplete="tel" maxLength={30} placeholder="0901 234 567" value={phone} onChange={(event) => setPhone(event.target.value)} disabled={loading}/></div>
              </>}
              <label className={mode === 'register' ? 'field-spaced' : ''} htmlFor="email">Email</label>
              <div className="input-wrap"><span className="input-icon"><MailIcon/></span><input id="email" name="email" type="email" autoComplete="email" placeholder="tenban@congty.vn" value={email} onChange={(event) => setEmail(event.target.value)} disabled={loading}/></div>
              <div className="label-row"><label htmlFor="password">Mật khẩu</label>{mode === 'login' && <button type="button" className="forgot-link" onClick={() => setError('Vui lòng liên hệ quản trị viên để được cấp lại mật khẩu.')}>Quên mật khẩu?</button>}</div>
              <div className="input-wrap"><span className="input-icon"><LockIcon/></span><input id="password" name="password" type={showPassword ? 'text' : 'password'} autoComplete={mode === 'login' ? 'current-password' : 'new-password'} placeholder="Nhập mật khẩu" value={password} onChange={(event) => setPassword(event.target.value)} disabled={loading}/><button className="password-toggle" type="button" aria-label={showPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'} onClick={() => setShowPassword((value) => !value)}><EyeIcon hidden={showPassword}/></button></div>
              {mode === 'register' && <>
                <div className="password-rules">{passwordRules.map((rule) => <span className={rule.test(password) ? 'passed' : ''} key={rule.label}><i>{rule.test(password) ? '✓' : '·'}</i>{rule.label}</span>)}</div>
                <label className="field-spaced" htmlFor="confirmPassword">Xác nhận mật khẩu</label>
                <div className="input-wrap"><span className="input-icon"><LockIcon/></span><input id="confirmPassword" name="confirmPassword" type={showPassword ? 'text' : 'password'} autoComplete="new-password" placeholder="Nhập lại mật khẩu" value={confirmPassword} onChange={(event) => setConfirmPassword(event.target.value)} disabled={loading}/></div>
                {otpSent && <div className="otp-panel"><span>Mã xác thực đã được gửi đến <b>{email}</b>. Mã có hiệu lực trong 5 phút.</span><label htmlFor="otp">Mã OTP</label><input id="otp" inputMode="numeric" autoComplete="one-time-code" maxLength={6} placeholder="000000" value={otp} onChange={(event) => setOtp(event.target.value.replace(/\D/g, '').slice(0, 6))} disabled={loading}/><button type="button" onClick={async () => { setLoading(true); setError(''); try { await requestRegistrationOtp(email.trim()); setOtp('') } catch (reason) { setError(reason instanceof Error ? reason.message : 'Không thể gửi lại OTP.') } finally { setLoading(false) } }}>Gửi lại mã</button></div>}
              </>}
              {mode === 'login' && <label className="remember-row"><input type="checkbox" checked={rememberEmail} onChange={(event) => setRememberEmail(event.target.checked)}/><span>Ghi nhớ email trên thiết bị này</span></label>}
              <button className={`primary-button ${mode === 'register' ? 'register-submit' : ''}`} type="submit" disabled={loading}>{loading ? <><i className="spinner"/>{mode === 'login' ? 'Đang xác thực...' : otpSent ? 'Đang xác minh...' : 'Đang gửi OTP...'}</> : <>{mode === 'login' ? 'Đăng nhập' : otpSent ? 'Xác minh và tạo tài khoản' : 'Gửi mã OTP'} <span>→</span></>}</button>
            </form>
            <p className="auth-switch">{mode === 'login' ? 'Chưa có tài khoản?' : 'Đã có tài khoản?'} <button type="button" onClick={() => switchMode(mode === 'login' ? 'register' : 'login')}>{mode === 'login' ? 'Đăng ký ngay' : 'Đăng nhập'}</button></p>
            <div className="security-note"><span>✓</span><p><b>Kết nối được bảo vệ</b><small>Phiên đăng nhập sẽ kết thúc khi bạn đóng trình duyệt.</small></p></div>
          </div>
        )}
        <footer className="form-footer"><span>Cần hỗ trợ?</span><a href="mailto:support@transflow.vn">Liên hệ quản trị viên</a></footer>
      </section>
    </main>
  )
}

export default App
