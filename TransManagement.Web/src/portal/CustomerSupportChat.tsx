import { FormEvent, useCallback, useEffect, useRef, useState } from 'react'
import type { HubConnection } from '@microsoft/signalr'
import type { UserProfile } from '../types'
import { customerChatApi } from './chat-api'
import { createChatConnection, HubConnectionState } from './chat-realtime'
import type { ChatConversation, ChatMessage, ConversationChangedEvent, MessagesReadEvent } from './chat-types'
import './customer-chat.css'

interface Props {
  accessToken: string
  profile: UserProfile
}

const statusLabels: Record<ChatConversation['status'], string> = {
  Open: 'Đang chờ',
  Assigned: 'Đang hỗ trợ',
  Closed: 'Đã đóng',
}

const formatTime = (value?: string) => value
  ? new Intl.DateTimeFormat('vi-VN', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(value))
  : 'Chưa có tin nhắn'

export default function CustomerSupportChat({ accessToken, profile }: Props) {
  const [conversations, setConversations] = useState<ChatConversation[]>([])
  const [selectedId, setSelectedId] = useState<string>()
  const [messages, setMessages] = useState<ChatMessage[]>([])
  const [loading, setLoading] = useState(true)
  const [messagesLoading, setMessagesLoading] = useState(false)
  const [creating, setCreating] = useState(false)
  const [sending, setSending] = useState(false)
  const [showCreate, setShowCreate] = useState(false)
  const [connectionState, setConnectionState] = useState<'connecting' | 'online' | 'offline'>('connecting')
  const [error, setError] = useState('')
  const connectionRef = useRef<HubConnection | null>(null)
  const selectedIdRef = useRef<string | undefined>(undefined)
  const messageEndRef = useRef<HTMLDivElement>(null)

  const selected = conversations.find(item => item.id === selectedId)
  const totalUnread = conversations.reduce((total, item) => total + item.unreadCount, 0)

  const loadConversations = useCallback(async () => {
    try {
      const items = await customerChatApi.conversations(accessToken)
      setConversations(items)
      setSelectedId(current => current && items.some(item => item.id === current)
        ? current
        : items[0]?.id)
      setError('')
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Không thể tải các cuộc trò chuyện.')
    } finally {
      setLoading(false)
    }
  }, [accessToken])

  const markAsRead = useCallback(async (conversationId: string) => {
    try {
      await customerChatApi.markAsRead(accessToken, conversationId)
      setConversations(items => items.map(item => item.id === conversationId
        ? { ...item, unreadCount: 0 }
        : item))
    } catch {
      // The next successful refresh will retry the unread state.
    }
  }, [accessToken])

  const loadMessages = useCallback(async (conversationId: string) => {
    setMessagesLoading(true)
    try {
      const items = await customerChatApi.messages(accessToken, conversationId)
      if (selectedIdRef.current === conversationId) setMessages(items)
      await markAsRead(conversationId)
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Không thể tải nội dung trò chuyện.')
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
    connection.on('conversationChanged', (event: ConversationChangedEvent) => {
      void loadConversations()
      if (event.change === 'messageSent' && event.conversationId === selectedIdRef.current)
        void loadMessages(event.conversationId)
    })
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

  async function createConversation(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const form = event.currentTarget
    const data = new FormData(form)
    const subject = String(data.get('subject') ?? '').trim()
    const initialMessage = String(data.get('initialMessage') ?? '').trim()
    if (!subject || !initialMessage) return

    setCreating(true)
    setError('')
    try {
      const conversation = await customerChatApi.createConversation(accessToken, subject, initialMessage)
      form.reset()
      setShowCreate(false)
      await loadConversations()
      await selectConversation(conversation.id)
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Không thể tạo yêu cầu hỗ trợ.')
    } finally {
      setCreating(false)
    }
  }

  async function sendMessage(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!selectedId || selected?.status === 'Closed') return
    const form = event.currentTarget
    const data = new FormData(form)
    const content = String(data.get('message') ?? '').trim()
    if (!content) return

    setSending(true)
    setError('')
    try {
      const message = await customerChatApi.sendMessage(accessToken, selectedId, content)
      setMessages(items => items.some(item => item.id === message.id) ? items : [...items, message])
      form.reset()
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Không thể gửi tin nhắn.')
    } finally {
      setSending(false)
    }
  }

  return <section className="customer-support" aria-labelledby="customer-support-title">
    <header className="customer-support__heading">
      <div><p>HỖ TRỢ KHÁCH HÀNG</p><h2 id="customer-support-title">Hỗ trợ trực tuyến</h2><span>Trao đổi trực tiếp với bộ phận quản trị về đơn hàng và dịch vụ vận chuyển.</span></div>
      <div className={`chat-connection chat-connection--${connectionState}`}><i/>{connectionState === 'online' ? 'Trực tuyến' : connectionState === 'connecting' ? 'Đang kết nối lại' : 'Tạm mất kết nối'}</div>
    </header>

    {error && <div className="chat-error" role="alert">{error}<button type="button" onClick={() => setError('')}>×</button></div>}

    <div className="customer-support__layout">
      <aside className="chat-sidebar">
        <div className="chat-sidebar__top"><div><b>Cuộc trò chuyện</b>{totalUnread > 0 && <span>{totalUnread}</span>}</div><button type="button" onClick={() => setShowCreate(value => !value)}>{showCreate ? 'Đóng' : '+ Tạo mới'}</button></div>
        {showCreate && <form className="chat-create" onSubmit={createConversation}>
          <label>Chủ đề<input name="subject" maxLength={200} required placeholder="Ví dụ: Cần hỗ trợ đơn hàng"/></label>
          <label>Nội dung<textarea name="initialMessage" maxLength={2000} required rows={4} placeholder="Mô tả vấn đề bạn cần hỗ trợ..."/></label>
          <button disabled={creating}>{creating ? 'Đang gửi...' : 'Gửi yêu cầu'}</button>
        </form>}
        <div className="chat-conversation-list">
          {loading ? <div className="chat-list-state">Đang tải...</div> : conversations.length === 0 ? <div className="chat-list-state"><b>Chưa có cuộc trò chuyện</b><span>Hãy tạo yêu cầu khi bạn cần hỗ trợ.</span></div> : conversations.map(item => <button type="button" className={item.id === selectedId ? 'active' : ''} onClick={() => void selectConversation(item.id)} key={item.id}>
            <span className="chat-avatar">TF</span><span className="chat-conversation-copy"><b>{item.subject}</b><small>{item.assignedAdminName ? `Hỗ trợ bởi ${item.assignedAdminName}` : 'Đang chờ quản trị viên tiếp nhận'}</small><em>{formatTime(item.lastMessageAtUtc || item.createdAtUtc)}</em></span>{item.unreadCount > 0 && <i>{item.unreadCount}</i>}
          </button>)}
        </div>
      </aside>

      <div className="chat-window">
        {!selected ? <div className="chat-window__empty"><span>✦</span><h3>TransFlow luôn sẵn sàng hỗ trợ</h3><p>Chọn một cuộc trò chuyện hoặc tạo yêu cầu mới để bắt đầu.</p></div> : <>
          <header className="chat-window__header"><div><span>TF</span><div><b>{selected.subject}</b><small>{selected.assignedAdminName || 'Bộ phận hỗ trợ TransFlow'}</small></div></div><em className={`chat-status chat-status--${selected.status.toLowerCase()}`}>{statusLabels[selected.status]}</em></header>
          <div className="chat-messages" aria-live="polite">
            {messagesLoading ? <div className="chat-window__empty compact">Đang tải tin nhắn...</div> : messages.length === 0 ? <div className="chat-window__empty compact">Chưa có tin nhắn.</div> : messages.map(message => {
              const own = message.senderUserId === profile.id
              return <article className={`chat-message ${own ? 'chat-message--own' : ''}`} key={message.id}><div><small>{own ? 'Bạn' : message.senderName}</small><p>{message.content}</p><footer><time>{formatTime(message.sentAtUtc)}</time>{own && <span>{message.readAtUtc ? 'Đã đọc' : 'Đã gửi'}</span>}</footer></div></article>
            })}
            <div ref={messageEndRef}/>
          </div>
          {selected.status === 'Closed' ? <div className="chat-closed">Cuộc trò chuyện đã được đóng. Vui lòng tạo yêu cầu mới nếu bạn cần thêm hỗ trợ.</div> : <form className="chat-composer" onSubmit={sendMessage}><textarea name="message" maxLength={2000} rows={2} required placeholder="Nhập tin nhắn..." aria-label="Nội dung tin nhắn"/><button disabled={sending}>{sending ? 'Đang gửi' : 'Gửi'} <span>→</span></button></form>}
        </>}
      </div>
    </div>
  </section>
}
