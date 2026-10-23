import { FormEvent, useCallback, useEffect, useMemo, useRef, useState } from 'react'
import type { HubConnection } from '@microsoft/signalr'
import type { UserProfile } from '../types'
import { adminChatApi } from '../portal/chat-api'
import { createChatConnection, HubConnectionState } from '../portal/chat-realtime'
import type { ChatConversation, ChatMessage, ConversationChangedEvent, ConversationStatus, MessagesReadEvent } from '../portal/chat-types'
import './admin-support.css'
import './admin-support-scroll.css'

interface Props {
  accessToken: string
  profile: UserProfile
  search: string
  onUnreadChange: (count: number) => void
}

type StatusFilter = 'All' | ConversationStatus

const statusLabels: Record<ConversationStatus, string> = {
  Open: 'Chờ tiếp nhận',
  Assigned: 'Đang xử lý',
  Closed: 'Đã đóng',
}

const formatTime = (value?: string) => value
  ? new Intl.DateTimeFormat('vi-VN', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(value))
  : '—'

export default function AdminSupportInbox({ accessToken, profile, search, onUnreadChange }: Props) {
  const [conversations, setConversations] = useState<ChatConversation[]>([])
  const [selectedId, setSelectedId] = useState<string>()
  const [messages, setMessages] = useState<ChatMessage[]>([])
  const [filter, setFilter] = useState<StatusFilter>('All')
  const [loading, setLoading] = useState(true)
  const [messagesLoading, setMessagesLoading] = useState(false)
  const [busy, setBusy] = useState('')
  const [error, setError] = useState('')
  const [connectionState, setConnectionState] = useState<'connecting' | 'online' | 'offline'>('connecting')
  const connectionRef = useRef<HubConnection | null>(null)
  const selectedIdRef = useRef<string | undefined>(undefined)
  const messageEndRef = useRef<HTMLDivElement>(null)

  const selected = conversations.find(item => item.id === selectedId)
  const counts = useMemo(() => ({
    All: conversations.length,
    Open: conversations.filter(item => item.status === 'Open').length,
    Assigned: conversations.filter(item => item.status === 'Assigned').length,
    Closed: conversations.filter(item => item.status === 'Closed').length,
  }), [conversations])
  const visibleConversations = useMemo(() => {
    const term = search.trim().toLocaleLowerCase('vi')
    return conversations.filter(item =>
      (filter === 'All' || item.status === filter) &&
      (!term || item.subject.toLocaleLowerCase('vi').includes(term) || item.customerName.toLocaleLowerCase('vi').includes(term)))
  }, [conversations, filter, search])

  useEffect(() => {
    if (selectedId && visibleConversations.some(item => item.id === selectedId)) return
    setSelectedId(visibleConversations[0]?.id)
  }, [selectedId, visibleConversations])

  const loadConversations = useCallback(async () => {
    try {
      const items = await adminChatApi.conversations(accessToken)
      setConversations(items)
      onUnreadChange(items.reduce((total, item) => total + item.unreadCount, 0))
      setSelectedId(current => current && items.some(item => item.id === current) ? current : items[0]?.id)
      setError('')
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Không thể tải hộp thư hỗ trợ.')
    } finally {
      setLoading(false)
    }
  }, [accessToken, onUnreadChange])

  const markAsRead = useCallback(async (conversationId: string) => {
    try {
      await adminChatApi.markAsRead(accessToken, conversationId)
      setConversations(items => {
        const next = items.map(item => item.id === conversationId ? { ...item, unreadCount: 0 } : item)
        onUnreadChange(next.reduce((total, item) => total + item.unreadCount, 0))
        return next
      })
    } catch {
      // A later refresh will retry the read state.
    }
  }, [accessToken, onUnreadChange])

  const loadMessages = useCallback(async (conversationId: string) => {
    setMessagesLoading(true)
    try {
      const items = await adminChatApi.messages(accessToken, conversationId)
      if (selectedIdRef.current === conversationId) setMessages(items)
      await markAsRead(conversationId)
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Không thể tải tin nhắn.')
    } finally {
      setMessagesLoading(false)
    }
  }, [accessToken, markAsRead])

  const joinConversation = useCallback(async (conversationId: string) => {
    const connection = connectionRef.current
    if (!connection || connection.state !== HubConnectionState.Connected) return
    await connection.invoke('JoinConversation', conversationId)
  }, [])

  useEffect(() => { void loadConversations() }, [loadConversations])

  useEffect(() => {
    const previousId = selectedIdRef.current
    selectedIdRef.current = selectedId
    if (!selectedId) {
      setMessages([])
      return
    }
    if (previousId && previousId !== selectedId)
      void connectionRef.current?.invoke('LeaveConversation', previousId).catch(() => undefined)
    void joinConversation(selectedId).catch(() => setConnectionState('offline'))
    void loadMessages(selectedId)
  }, [selectedId, joinConversation, loadMessages])

  useEffect(() => {
    const connection = createChatConnection(accessToken)
    let disposed = false
    let starting = false
    let retryTimer: number | undefined
    connectionRef.current = connection

    const restoreConversation = async () => {
      if (disposed) return
      setConnectionState('online')
      if (selectedIdRef.current) {
        await joinConversation(selectedIdRef.current)
        await loadMessages(selectedIdRef.current)
      }
      await loadConversations()
    }

    const scheduleStart = () => {
      if (disposed || retryTimer !== undefined) return
      setConnectionState('offline')
      retryTimer = window.setTimeout(() => {
        retryTimer = undefined
        void startConnection()
      }, 5_000)
    }

    const startConnection = async () => {
      if (disposed || starting || connection.state !== HubConnectionState.Disconnected) return
      starting = true
      setConnectionState('connecting')
      try {
        await connection.start()
        await restoreConversation()
      } catch {
        scheduleStart()
      } finally {
        starting = false
      }
    }

    connection.on('messageReceived', (message: ChatMessage) => {
      if (message.conversationId === selectedIdRef.current) {
        setMessages(items => items.some(item => item.id === message.id) ? items : [...items, message])
        if (message.senderUserId !== profile.id) void markAsRead(message.conversationId)
      }
    })
    connection.on('conversationChanged', (_event: ConversationChangedEvent) => void loadConversations())
    connection.on('messagesRead', (event: MessagesReadEvent) => {
      if (event.conversationId !== selectedIdRef.current || event.readByUserId === profile.id) return
      setMessages(items => items.map(message => message.senderUserId === profile.id && !message.readAtUtc
        ? { ...message, readAtUtc: event.readAtUtc }
        : message))
    })
    connection.onreconnecting(() => setConnectionState('connecting'))
    connection.onreconnected(() => restoreConversation())
    connection.onclose(() => scheduleStart())
    void startConnection()

    return () => {
      disposed = true
      if (retryTimer !== undefined) window.clearTimeout(retryTimer)
      connectionRef.current = null
      void connection.stop()
    }
  }, [accessToken, joinConversation, loadConversations, markAsRead, profile.id])

  useEffect(() => {
    messageEndRef.current?.scrollIntoView({ behavior: 'smooth', block: 'nearest' })
  }, [messages])

  async function selectConversation(conversationId: string) {
    const previousId = selectedIdRef.current
    if (previousId && previousId !== conversationId && connectionRef.current?.state === HubConnectionState.Connected)
      await connectionRef.current.invoke('LeaveConversation', previousId).catch(() => undefined)
    selectedIdRef.current = conversationId
    setSelectedId(conversationId)
  }

  async function sendMessage(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!selectedId || selected?.status === 'Closed') return
    const form = event.currentTarget
    const content = String(new FormData(form).get('message') ?? '').trim()
    if (!content) return
    setBusy('send')
    setError('')
    try {
      const message = await adminChatApi.sendMessage(accessToken, selectedId, content)
      setMessages(items => items.some(item => item.id === message.id) ? items : [...items, message])
      form.reset()
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Không thể gửi tin nhắn.')
    } finally {
      setBusy('')
    }
  }

  async function updateConversation(action: 'assign' | 'close' | 'reopen') {
    if (!selectedId) return
    setBusy(action)
    setError('')
    try {
      if (action === 'assign') await adminChatApi.assign(accessToken, selectedId)
      if (action === 'close') await adminChatApi.close(accessToken, selectedId)
      if (action === 'reopen') await adminChatApi.reopen(accessToken, selectedId)
      await loadConversations()
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Không thể cập nhật hội thoại.')
    } finally {
      setBusy('')
    }
  }

  return <section className="admin-support-inbox">
    <div className="admin-support-toolbar">
      <div className="admin-support-filters">{(['All','Open','Assigned','Closed'] as StatusFilter[]).map(value => <button type="button" className={filter === value ? 'active' : ''} onClick={() => setFilter(value)} key={value}>{value === 'All' ? 'Tất cả' : statusLabels[value]} <span>{counts[value]}</span></button>)}</div>
      <div className={`admin-chat-connection admin-chat-connection--${connectionState}`}><i/>{connectionState === 'online' ? 'Trực tuyến' : connectionState === 'connecting' ? 'Đang kết nối lại' : 'Mất kết nối'}</div>
    </div>
    {error && <div className="admin-chat-error" role="alert">{error}<button type="button" onClick={() => setError('')}>×</button></div>}

    <div className="admin-support-layout">
      <aside className="admin-conversation-list">
        <header><div><b>Hộp thư</b><span>{visibleConversations.length} hội thoại</span></div><button type="button" onClick={() => void loadConversations()} aria-label="Làm mới">↻</button></header>
        <div>{loading ? <p className="admin-chat-empty">Đang tải...</p> : visibleConversations.length === 0 ? <p className="admin-chat-empty">Không có hội thoại phù hợp.</p> : visibleConversations.map(item => <button type="button" className={item.id === selectedId ? 'active' : ''} onClick={() => void selectConversation(item.id)} key={item.id}>
          <span className="admin-customer-avatar">{item.customerName.charAt(0).toUpperCase()}</span><span><b>{item.customerName}</b><strong>{item.subject}</strong><small>{item.assignedAdminName ? `Phụ trách: ${item.assignedAdminName}` : 'Chưa có người tiếp nhận'} · {formatTime(item.lastMessageAtUtc || item.createdAtUtc)}</small></span>{item.unreadCount > 0 && <i>{item.unreadCount}</i>}
        </button>)}</div>
      </aside>

      <div className="admin-chat-window">
        {!selected ? <div className="admin-chat-placeholder"><span>✦</span><b>Chọn một hội thoại</b><p>Nội dung trao đổi với khách hàng sẽ hiển thị tại đây.</p></div> : <>
          <header className="admin-chat-header"><div><span className="admin-customer-avatar">{selected.customerName.charAt(0).toUpperCase()}</span><div><b>{selected.subject}</b><small>{selected.customerName} · {statusLabels[selected.status]}</small></div></div><div>{!selected.assignedAdminUserId && <button disabled={!!busy} onClick={() => void updateConversation('assign')}>Tiếp nhận</button>}{selected.status !== 'Closed' ? <button className="close" disabled={!!busy} onClick={() => void updateConversation('close')}>Đóng hội thoại</button> : <button disabled={!!busy} onClick={() => void updateConversation('reopen')}>Mở lại</button>}</div></header>
          <div className="admin-chat-messages" aria-live="polite">{messagesLoading ? <div className="admin-chat-placeholder compact">Đang tải tin nhắn...</div> : messages.map(message => {
            const own = message.senderUserId === profile.id
            return <article className={own ? 'own' : ''} key={message.id}><div><small>{own ? 'Bạn' : message.senderName}</small><p>{message.content}</p><footer>{formatTime(message.sentAtUtc)}{own && <span>{message.readAtUtc ? 'Đã đọc' : 'Đã gửi'}</span>}</footer></div></article>
          })}<div ref={messageEndRef}/></div>
          {selected.status === 'Closed' ? <div className="admin-chat-closed">Hội thoại đã đóng. Mở lại để tiếp tục phản hồi khách hàng.</div> : <form className="admin-chat-composer" onSubmit={sendMessage}><textarea name="message" rows={2} maxLength={2000} required placeholder="Nhập nội dung phản hồi..."/><button disabled={!!busy}>{busy === 'send' ? 'Đang gửi...' : 'Gửi phản hồi'} <span>→</span></button></form>}
        </>}
      </div>

      <aside className="admin-chat-details">
        <h3>Thông tin hỗ trợ</h3>{selected ? <><div className="admin-detail-profile"><span>{selected.customerName.charAt(0).toUpperCase()}</span><b>{selected.customerName}</b><small>Khách hàng TransFlow</small></div><dl><div><dt>Trạng thái</dt><dd className={`state-${selected.status.toLowerCase()}`}>{statusLabels[selected.status]}</dd></div><div><dt>Người phụ trách</dt><dd>{selected.assignedAdminName || 'Chưa tiếp nhận'}</dd></div><div><dt>Tạo lúc</dt><dd>{formatTime(selected.createdAtUtc)}</dd></div><div><dt>Cập nhật cuối</dt><dd>{formatTime(selected.lastMessageAtUtc || selected.updatedAtUtc)}</dd></div></dl><div className="admin-detail-note"><b>Nguyên tắc hỗ trợ</b><p>Không yêu cầu khách hàng cung cấp mật khẩu, mã OTP hoặc thông tin thanh toán nhạy cảm qua chat.</p></div></> : <p className="admin-chat-empty">Chọn hội thoại để xem thông tin.</p>}
      </aside>
    </div>
  </section>
}
