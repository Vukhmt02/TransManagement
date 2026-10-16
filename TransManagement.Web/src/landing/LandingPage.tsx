import { FormEvent, useRef, useState } from 'react'
import './landing.css'
import './services-offers.css'

interface Props { onLogin: () => void; onRegister: () => void }

type IconName = 'arrow' | 'box' | 'check' | 'clock' | 'location' | 'phone' | 'route' | 'search' | 'shield' | 'truck'

const services = [
  { eyebrow: 'VẬN CHUYỂN LIÊN TỈNH',  copy: 'Tổ chức hành trình rõ ràng từ điểm nhận đến điểm giao.', image: '/images/services/intercity-freight.png', alt: 'Xe tải vận chuyển hàng hóa liên tỉnh' },
  { eyebrow: 'HÀNG NGUYÊN CHUYẾN',  copy: 'Linh hoạt phương tiện, thời gian khởi hành và kế hoạch giao nhận.', image: '/images/services/full-truckload.png', alt: 'Đội xe tải phục vụ hàng nguyên chuyến' },
  { eyebrow: 'THEO DÕI HÀNH TRÌNH',  copy: 'Cập nhật GPS giúp điều phối theo dõi tiến độ tập trung.', image: '/images/services/gps-visibility.png', alt: 'Trung tâm theo dõi vị trí và hành trình xe tải' },
  { eyebrow: 'GIAO NHẬN MINH BẠCH',  copy: 'Ghi nhận người nhận, thời gian và bằng chứng giao hàng.', image: '/images/services/delivery-confirmation.png', alt: 'Tài xế xác nhận giao hàng với khách hàng' },
]

const routes = [
  { from: 'TP. Hồ Chí Minh', to: 'Đà Nẵng', type: 'Tuyến minh họa', time: 'Theo kế hoạch chuyến' },
  { from: 'Hà Nội', to: 'Hải Phòng', type: 'Tuyến minh họa', time: 'Theo kế hoạch chuyến' },
  { from: 'Bình Dương', to: 'Đồng Nai', type: 'Tuyến minh họa', time: 'Theo kế hoạch chuyến' },
]

const faqs = [
  ['Tôi theo dõi đơn hàng bằng cách nào?', 'Đăng nhập cổng khách hàng để xem trạng thái và thông tin của các đơn vận chuyển thuộc tài khoản của bạn.'],
  ['Chi phí vận chuyển được xác định ra sao?', 'Chi phí phụ thuộc tuyến đường, khối lượng, thể tích, phương tiện và lịch giao nhận. Bộ phận điều phối sẽ xác nhận trước khi vận chuyển.'],
  ['Khi nào tôi có thể hủy đơn?', 'Bạn có thể hủy khi đơn vẫn ở trạng thái nháp hoặc đang chờ phân công.'],
  ['Tài xế cập nhật hành trình như thế nào?', 'Sau khi bắt đầu chuyến, tài xế có thể chia sẻ GPS và xác nhận từng điểm lấy hoặc giao hàng.'],
]

function Icon({ name }: { name: IconName }) {
  const paths: Record<IconName, React.ReactNode> = {
    arrow: <path d="M5 12h14m-5-5 5 5-5 5"/>,
    box: <><path d="m4 7 8-4 8 4-8 4-8-4Z"/><path d="M4 7v10l8 4 8-4V7M12 11v10"/></>,
    check: <><circle cx="12" cy="12" r="9"/><path d="m8 12 2.5 2.5L16 9"/></>,
    clock: <><circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/></>,
    location: <><path d="M20 10c0 5-8 11-8 11S4 15 4 10a8 8 0 1 1 16 0Z"/><circle cx="12" cy="10" r="2.5"/></>,
    phone: <path d="M7 3H4.5A1.5 1.5 0 0 0 3 4.5C3 13.6 10.4 21 19.5 21a1.5 1.5 0 0 0 1.5-1.5V17l-4-1-1.2 2a14 14 0 0 1-9.8-9.8L8 7 7 3Z"/>,
    route: <><circle cx="6" cy="18" r="3"/><circle cx="18" cy="6" r="3"/><path d="M8.5 16.5c5-2 2-6 7-8"/></>,
    search: <><circle cx="10.5" cy="10.5" r="6.5"/><path d="m16 16 5 5"/></>,
    shield: <><path d="M12 3 5 6v5c0 5 3 8 7 10 4-2 7-5 7-10V6l-7-3Z"/><path d="m9 12 2 2 4-4"/></>,
    truck: <><path d="M3 6h11v11H3zM14 10h4l3 4v3h-7z"/><circle cx="7" cy="19" r="2"/><circle cx="18" cy="19" r="2"/></>,
  }
  return <svg viewBox="0 0 24 24" aria-hidden="true">{paths[name]}</svg>
}

export default function LandingPage({ onLogin, onRegister }: Props) {
  const [menu, setMenu] = useState(false)
  const [tracking, setTracking] = useState('')
  const [notice, setNotice] = useState('')
  const [quoteSent, setQuoteSent] = useState(false)
  const [servicePage, setServicePage] = useState(0)
  const serviceCarousel = useRef<HTMLDivElement>(null)

  function scrollServices(direction: -1 | 1) {
    const viewport = serviceCarousel.current
    if (!viewport) return
    viewport.scrollBy({ left: direction * viewport.clientWidth * 0.88, behavior: 'smooth' })
  }

  function updateServicePage() {
    const viewport = serviceCarousel.current
    if (!viewport) return
    const maxScroll = viewport.scrollWidth - viewport.clientWidth
    setServicePage(maxScroll > 0 && viewport.scrollLeft > maxScroll / 2 ? 1 : 0)
  }

  function goToServicePage(page: number) {
    const viewport = serviceCarousel.current
    if (!viewport) return
    viewport.scrollTo({ left: page === 0 ? 0 : viewport.scrollWidth, behavior: 'smooth' })
    setServicePage(page)
  }

  function track(event: FormEvent) {
    event.preventDefault()
    setNotice(tracking.trim() ? 'Đăng nhập để xem thông tin vận đơn được bảo vệ.' : 'Hãy nhập mã vận đơn cần tra cứu.')
  }

  function quote(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setQuoteSent(true)
    event.currentTarget.reset()
  }

  return <div className="landing">
    <div className="landing-topbar"><div><span><Icon name="phone"/>1900 1234</span><span>support@transflow.vn</span></div><p>Hỗ trợ vận hành · Theo dõi hành trình tập trung</p></div>
    <header className="landing-header">
      <a className="landing-logo" href="#top"><span><Icon name="truck"/></span><b>TRANS<em>FLOW</em></b></a>
      <button className="landing-menu" onClick={() => setMenu(!menu)} aria-label="Mở menu" aria-expanded={menu}>☰</button>
      <nav className={menu ? 'open' : ''}>
        <a href="#top" onClick={() => setMenu(false)}>Trang chủ</a><a href="#routes" onClick={() => setMenu(false)}>Tuyến vận chuyển</a><a href="#services" onClick={() => setMenu(false)}>Dịch vụ</a><a href="#process" onClick={() => setMenu(false)}>Quy trình</a><a href="#contact" onClick={() => setMenu(false)}>Liên hệ</a>
      </nav>
      <div className="landing-auth"><button onClick={onLogin}>Đăng nhập</button><button onClick={onRegister}>Đăng ký</button></div>
    </header>

    <main id="top">
      <section className="transport-banner" aria-label="Vận chuyển hàng hóa toàn quốc">
        <div className="transport-banner__content">
          <p><span/> VẬN CHUYỂN HÀNG HÓA TOÀN QUỐC</p>
          <h2>Hà Nội <i>⇄</i> Đà Nẵng <i>⇄</i> TP. Hồ Chí Minh</h2>
          <span>Kết nối hành trình, theo dõi tập trung và giao nhận rõ ràng trên một nền tảng.</span>
          <div>
            <button onClick={onRegister}>Gửi yêu cầu vận chuyển </button>
            <button type="button" onClick={() => document.getElementById('tracking')?.scrollIntoView({ behavior: 'smooth' })}>Tra cứu vận đơn <Icon name="search"/></button>
          </div>
        </div>
      </section>
      <section className="landing-hero">
        <h1>Giải pháp quản lí vận tải</h1>
        <div className="hero-route-lines" aria-hidden="true"/>
        {/* <div className="hero-lookup" id="tracking">
          <div className="lookup-title"><span><Icon name="search"/></span><div><small>TRA CỨU NHANH</small><h2>Kiểm tra vận đơn</h2></div></div>
          <p>Nhập mã đơn để bắt đầu. Thông tin chi tiết chỉ hiển thị sau khi đăng nhập.</p>
          <form onSubmit={track}><label htmlFor="landing-tracking">Mã vận đơn</label><div><Icon name="box"/><input id="landing-tracking" value={tracking} onChange={event => setTracking(event.target.value)} placeholder="VD: WEB-20261009-AB12"/><button>Tra cứu <Icon name="arrow"/></button></div></form>
          {notice && <div className="lookup-notice">{notice}{tracking.trim() && <button onClick={onLogin}>Đăng nhập ngay</button>}</div>}
          <div className="lookup-route"><span><i/>Điểm nhận</span><b/><span><i/>Điểm giao</span></div>
          <div className="lookup-help"><Icon name="phone"/><span>Cần hỗ trợ?<b>1900 1234</b></span></div>
        </div> */}
      </section>

      <section className="capability-strip"><div><Icon name="clock"/><span><b>Điều phối tập trung</b><small>Theo dõi kế hoạch chuyến</small></span></div><div><Icon name="truck"/><span><b>Quản lý đội xe</b><small>Phương tiện và tài xế</small></span></div><div><Icon name="location"/><span><b>Cập nhật vị trí</b><small>Trong quá trình vận chuyển</small></span></div><div><Icon name="check"/><span><b>Giao nhận rõ ràng</b><small>Trạng thái và bằng chứng</small></span></div></section>
      <section className="landing-section route-showcase" id="routes">
        <div className="section-heading split-heading"><div><p>TUYẾN VẬN CHUYỂN</p><h2>Hành trình được tổ chức rõ ràng từ đầu đến cuối</h2></div><span>Các thẻ dưới đây minh họa cách tuyến đường được trình bày trong hệ thống, không phải lịch chạy cố định.</span></div>
        <div className="route-grid">{routes.map((route, index) => <article key={route.from}><header><span>0{index + 1}</span><small>{route.type}</small></header><div className="route-points"><div><i/><span><small>Điểm đi</small><b>{route.from}</b></span></div><em/><div><i/><span><small>Điểm đến</small><b>{route.to}</b></span></div></div><footer><span><Icon name="clock"/>{route.time}</span><button onClick={onRegister}>Gửi yêu cầu <Icon name="arrow"/></button></footer></article>)}</div>
      </section>

      {/* <section className="about-section">
        <div className="about-visual" aria-hidden="true"><div className="about-map"/><span className="about-truck"><Icon name="truck"/></span><div className="about-status"><i/><span><small>HỆ THỐNG VẬN HÀNH</small><b>Đang kết nối hành trình</b></span></div></div>
        <div className="about-copy"><p>VỀ TRANSFLOW</p><h2>Một trung tâm điều hành cho toàn bộ hoạt động vận tải</h2><span>TransFlow giúp doanh nghiệp quản lý khách hàng, đơn hàng, chuyến xe, tài xế và phương tiện mà không phải tách rời dữ liệu ở nhiều nơi.</span><ul><li><Icon name="check"/>Tạo và quản lý yêu cầu vận chuyển</li><li><Icon name="check"/>Phân công phương tiện và tài xế</li><li><Icon name="check"/>Theo dõi tiến độ từng chuyến</li></ul><a href="#process">Xem quy trình vận hành <Icon name="arrow"/></a></div>
      </section> */}

      <section className="landing-section services offers-showcase" id="services">
        <div className="offers-heading"><h2>DỊCH VỤ &amp; ƯU ĐÃI</h2><i/><p>Giải pháp vận chuyển được tổ chức theo từng nhu cầu và theo dõi tập trung trên TransFlow.</p></div>
        <div className="offers-slider">
          <button className="offers-arrow offers-arrow--prev" type="button" onClick={() => scrollServices(-1)} aria-label="Xem dịch vụ trước"><Icon name="arrow"/></button>
          <div className="offers-viewport" ref={serviceCarousel} onScroll={updateServicePage}>
            {services.map((service, index) => <article className="service-promo-card" key={service.title}>
              <img src={service.image} alt={service.alt} loading={index === 0 ? 'eager' : 'lazy'}/>
              <div className="service-promo-card__shade"/>
              <div className="service-promo-card__copy"><small>{service.eyebrow}</small><h3>{service.title}</h3><p>{service.copy}</p><a href="#quote">Yêu cầu tư vấn <Icon name="arrow"/></a></div>
            </article>)}
          </div>
          <button className="offers-arrow offers-arrow--next" type="button" onClick={() => scrollServices(1)} aria-label="Xem dịch vụ tiếp theo"><Icon name="arrow"/></button>
        </div>
        <div className="offers-dots" aria-label="Chọn nhóm dịch vụ"><button className={servicePage === 0 ? 'active' : ''} onClick={() => goToServicePage(0)} aria-label="Nhóm dịch vụ đầu"/><button className={servicePage === 1 ? 'active' : ''} onClick={() => goToServicePage(1)} aria-label="Nhóm dịch vụ tiếp theo"/></div>
      </section>

      <section className="landing-section process" id="process"><div className="section-heading"><p>QUY TRÌNH VẬN CHUYỂN</p><h2>5 bước xuyên suốt một hành trình</h2><span>Trạng thái được cập nhật theo từng giai đoạn để khách hàng và điều phối cùng theo dõi.</span></div><div className="process-line">{[['01','Gửi yêu cầu','Khách hàng nhập thông tin hàng hóa.'],['02','Xác nhận','Điều phối kiểm tra và lên phương án.'],['03','Phân công','Gắn đơn hàng với xe và tài xế.'],['04','Vận chuyển','Tài xế cập nhật hành trình, GPS.'],['05','Hoàn tất','Ghi nhận giao hàng và người nhận.']].map(([number,title,copy]) => <article key={number}><i>{number}</i><div><b>{title}</b><p>{copy}</p></div></article>)}
      </div></section>

      <section className="quote-section" id="quote"><div className="quote-copy"><p>YÊU CẦU TƯ VẤN</p><h2>Chuẩn bị cho chuyến hàng tiếp theo?</h2><span>Gửi thông tin cơ bản để bắt đầu. Đơn vận chuyển chính thức được tạo trong cổng khách hàng.</span><div><i><Icon name="phone"/></i><span><small>Đường dây hỗ trợ</small><b>1900 1234</b></span></div></div><form onSubmit={quote}><div className="quote-grid"><label>Họ và tên<input required placeholder="Nguyễn Văn An"/></label><label>Số điện thoại<input required type="tel" placeholder="0901 234 567"/></label><label>Điểm lấy hàng<input required placeholder="TP. Hồ Chí Minh"/></label><label>Điểm giao hàng<input required placeholder="Đà Nẵng"/></label><label>Loại hàng<input required placeholder="Hàng tiêu dùng"/></label><label>Khối lượng dự kiến<input required placeholder="500 kg"/></label></div><button>Gửi yêu cầu tư vấn <Icon name="arrow"/></button>{quoteSent && <p className="quote-success"><Icon name="check"/> Yêu cầu đã được ghi nhận trên giao diện. Đăng ký tài khoản để tạo đơn chính thức.</p>}</form></section>

      <section className="landing-section faq"><div className="section-heading"><p>HỖ TRỢ KHÁCH HÀNG</p><h2>Câu hỏi thường gặp</h2></div><div className="faq-list">{faqs.map(([question,answer],index) => <details key={question} open={index === 0}><summary>{question}<span>+</span></summary><p>{answer}</p></details>)}</div></section>

      {/* <section className="final-cta"><div><p>BẮT ĐẦU CÙNG TRANSFLOW</p><h2>Quản lý hành trình rõ ràng hơn ngay hôm nay.</h2></div><div><button onClick={onRegister}>Tạo tài khoản khách hàng <Icon name="arrow"/></button><button onClick={onLogin}>Đăng nhập hệ thống</button></div></section> */}
    </main>

    <footer id="contact"><div className="footer-brand"><a className="landing-logo" href="#top"><span><Icon name="truck"/></span><b>TRANS<em>FLOW</em></b></a><p>Nền tảng quản lý vận tải kết nối khách hàng, điều phối và tài xế trên cùng một hành trình.</p></div><div><b>Khám phá</b><a href="#routes">Tuyến vận chuyển</a><a href="#services">Dịch vụ</a><a href="#process">Quy trình</a></div><div><b>Liên hệ</b><a href="tel:19001234">1900 1234</a><a href="mailto:support@transflow.vn">support@transflow.vn</a></div><div><b>Tài khoản</b><button onClick={onLogin}>Đăng nhập</button><button onClick={onRegister}>Đăng ký khách hàng</button></div><small>© 2026 TransFlow · Transportation Management System</small></footer>
  </div>
}
