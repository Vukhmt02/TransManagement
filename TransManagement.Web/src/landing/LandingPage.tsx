import { FormEvent, useState } from 'react'
import './landing.css'

interface Props { onLogin: () => void; onRegister: () => void }

const services = [
  ['↗', 'Vận chuyển liên tỉnh', 'Kết nối hàng hóa giữa các tỉnh thành với lịch trình minh bạch và ổn định.'],
  ['▣', 'Hàng nguyên chuyến', 'Phương tiện riêng cho từng lô hàng, linh hoạt thời gian và điểm giao nhận.'],
  ['⌖', 'Theo dõi hành trình', 'Cập nhật trạng thái và vị trí chuyến xe trong suốt quá trình vận chuyển.'],
  ['✓', 'Giao nhận xác thực', 'Lưu bằng chứng giao hàng, người nhận và thời gian hoàn tất chính xác.'],
]

const faqs = [
  ['Tôi theo dõi đơn hàng bằng cách nào?', 'Đăng nhập cổng khách hàng để xem trạng thái, tiến trình và thông tin của tất cả đơn vận chuyển.'],
  ['Giá vận chuyển được tính như thế nào?', 'Cước phí phụ thuộc tuyến đường, khối lượng, thể tích, loại xe và thời gian giao nhận. Chúng tôi sẽ xác nhận trước khi vận chuyển.'],
  ['Tôi có thể hủy đơn không?', 'Bạn có thể hủy khi đơn đang ở trạng thái nháp hoặc chờ phân công.'],
  ['TransFlow có hỗ trợ giao hàng liên tỉnh?', 'Có. Hệ thống được thiết kế cho cả vận chuyển nội thành, liên tỉnh và hàng nguyên chuyến.'],
]

export default function LandingPage({ onLogin, onRegister }: Props) {
  const [menu, setMenu] = useState(false)
  const [tracking, setTracking] = useState('')
  const [notice, setNotice] = useState('')
  const [quoteSent, setQuoteSent] = useState(false)

  function track(event: FormEvent) {
    event.preventDefault()
    setNotice(tracking.trim() ? 'Vui lòng đăng nhập để tra cứu vận đơn an toàn.' : 'Hãy nhập mã vận đơn cần tra cứu.')
  }

  function quote(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setQuoteSent(true)
    event.currentTarget.reset()
  }

  return <div className="landing">
    <header className="landing-header">
      <a className="landing-logo" href="#top"><span>TF</span><b>TRANS<em>FLOW</em></b></a>
      <button className="landing-menu" onClick={() => setMenu(!menu)} aria-label="Mở menu">☰</button>
      <nav className={menu ? 'open' : ''}>
        <a href="#services" onClick={() => setMenu(false)}>Dịch vụ</a><a href="#process" onClick={() => setMenu(false)}>Quy trình</a><a href="#tracking" onClick={() => setMenu(false)}>Tra cứu</a><a href="#contact" onClick={() => setMenu(false)}>Liên hệ</a>
      </nav>
      <div className="landing-auth"><button onClick={onLogin}>Đăng nhập</button><button onClick={onRegister}>Nhận báo giá</button></div>
    </header>

    <main id="top">
      <section className="landing-hero">
        <div className="hero-copy"><p className="landing-eyebrow"><i/> VẬN HÀNH THÔNG MINH · GIAO HÀNG ĐÚNG HẸN</p><h1>Vận chuyển hàng hóa<br/><em>minh bạch trên mọi hành trình.</em></h1><p className="hero-lead">Quản lý đơn hàng, theo dõi chuyến xe và xác nhận giao nhận trên một nền tảng thống nhất dành cho doanh nghiệp hiện đại.</p><div className="hero-actions"><button onClick={onRegister}>Gửi yêu cầu vận chuyển <span>→</span></button><a href="#tracking">Tra cứu đơn hàng</a></div><div className="hero-proof"><span><b>98.7%</b> giao đúng hạn</span><span><b>24/7</b> theo dõi hành trình</span><span><b>63</b> tỉnh thành</span></div></div>
        <div className="hero-visual" aria-hidden="true"><div className="map-lines"/><div className="visual-card route-a"><small>CHUYẾN ĐANG CHẠY</small><b>SHP-2026-018</b><span>TP. Hồ Chí Minh → Đà Nẵng</span><div><i/><i/><i/></div></div><div className="visual-truck">▰<span>● ●</span></div><div className="visual-card delivery"><span>✓</span><div><small>GIAO HÀNG</small><b>Đã xác nhận</b></div></div><div className="location-pin">⌖</div></div>
      </section>

      <section className="trust-strip"><span>ĐƯỢC TIN DÙNG TRONG VẬN HÀNH</span><b>LOGISTICS</b><b>RETAIL</b><b>MANUFACTURING</b><b>E-COMMERCE</b></section>

      <section className="landing-section services" id="services"><div className="section-heading"><p>DỊCH VỤ CỦA CHÚNG TÔI</p><h2>Một nền tảng cho mọi nhu cầu vận chuyển</h2><span>Từ đơn hàng nhỏ đến chuyến xe đường dài, TransFlow giúp bạn kiểm soát toàn bộ quá trình.</span></div><div className="service-grid">{services.map(([icon,title,copy]) => <article key={title}><i>{icon}</i><h3>{title}</h3><p>{copy}</p><a href="#quote">Tìm hiểu thêm →</a></article>)}</div></section>

      <section className="tracking-section" id="tracking"><div><p>TRA CỨU NHANH</p><h2>Đơn hàng của bạn đang ở đâu?</h2><span>Nhập mã vận đơn. Thông tin chi tiết được bảo vệ trong tài khoản khách hàng.</span></div><form onSubmit={track}><div><span>⌕</span><input value={tracking} onChange={e => setTracking(e.target.value)} placeholder="Ví dụ: WEB-20261009-AB12"/><button>Tra cứu</button></div>{notice && <p>{notice} {tracking.trim() && <button type="button" onClick={onLogin}>Đăng nhập ngay →</button>}</p>}</form></section>

      <section className="landing-section process" id="process"><div className="section-heading"><p>QUY TRÌNH ĐƠN GIẢN</p><h2>Từ yêu cầu đến giao hàng trong 5 bước</h2></div><div className="process-line">{[['01','Gửi yêu cầu'],['02','Xác nhận báo giá'],['03','Phân công xe'],['04','Theo dõi hành trình'],['05','Giao và xác nhận']].map(([n,t],i) => <article key={n}><i>{n}</i><b>{t}</b><span>{i < 4 ? '→' : '✓'}</span></article>)}</div></section>

      <section className="quote-section" id="quote"><div className="quote-copy"><p>NHẬN BÁO GIÁ</p><h2>Bắt đầu hành trình tiếp theo cùng TransFlow</h2><span>Gửi thông tin cơ bản, đội ngũ điều phối sẽ liên hệ để tư vấn phương án phù hợp.</span><ul><li>Phản hồi nhanh chóng</li><li>Chi phí minh bạch</li><li>Không có phí ẩn</li></ul></div><form onSubmit={quote}><div className="quote-grid"><label>Họ và tên<input required placeholder="Nguyễn Văn An"/></label><label>Số điện thoại<input required type="tel" placeholder="0901 234 567"/></label><label>Điểm lấy hàng<input required placeholder="TP. Hồ Chí Minh"/></label><label>Điểm giao hàng<input required placeholder="Đà Nẵng"/></label><label>Loại hàng<input required placeholder="Hàng tiêu dùng"/></label><label>Khối lượng dự kiến<input required placeholder="500 kg"/></label></div><button>Gửi yêu cầu báo giá →</button>{quoteSent && <p className="quote-success">✓ Yêu cầu đã được ghi nhận trên giao diện. Đăng ký tài khoản để gửi đơn chính thức.</p>}</form></section>

      <section className="landing-section faq"><div className="section-heading"><p>CÂU HỎI THƯỜNG GẶP</p><h2>Thông tin bạn có thể cần</h2></div><div className="faq-list">{faqs.map(([q,a],i) => <details key={q} open={i===0}><summary>{q}<span>+</span></summary><p>{a}</p></details>)}</div></section>

      <section className="final-cta"><p>SẴN SÀNG BẮT ĐẦU?</p><h2>Để mỗi chuyến hàng trở nên đơn giản hơn.</h2><div><button onClick={onRegister}>Tạo tài khoản miễn phí</button><button onClick={onLogin}>Đăng nhập cổng khách hàng</button></div></section>
    </main>
    <footer id="contact"><a className="landing-logo" href="#top"><span>TF</span><b>TRANS<em>FLOW</em></b></a><p>Nền tảng quản lý vận tải minh bạch, hiệu quả và đáng tin cậy.</p><div><b>Liên hệ</b><a href="tel:19001234">1900 1234</a><a href="mailto:support@transflow.vn">support@transflow.vn</a></div><div><b>Thông tin</b><a href="#services">Dịch vụ</a><a href="#process">Quy trình</a></div><small>© 2026 TransFlow. All rights reserved.</small></footer>
  </div>
}
