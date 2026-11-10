import { FormEvent, useCallback, useEffect, useRef, useState } from 'react'
import type { HubConnection } from '@microsoft/signalr'
import type { UserProfile } from '../types'
import { customerChatApi } from './chat-api'
import { createChatConnection, HubConnectionState } from './chat-realtime'
import type { AiTypingEvent, ChatConversation, ChatMessage, ConversationChangedEvent, MessagesReadEvent } from './chat-types'
import './customer-chat.css'

interface Props {
  accessToken: string
  profile: UserProfile
}

const statusLabels: Record<ChatConversation['status'], string> = {
  Open: 'AI hỗ trợ · Chờ Admin',
  Assigned: 'Nhân viên đang hỗ trợ',
  Closed: 'Đã đóng',
}

const quickPrompts = [
  { label: '📦 Kiểm tra đơn hàng của tôi', text: 'Kiểm tra trạng thái các đơn hàng của tôi' },
  { label: '🚚 Báo giá hàng 1.500 kg', text: 'Tư vấn loại xe và báo giá vận chuyển hàng 1500kg' },
  { label: '❌ Quy định hủy đơn', text: 'Tôi muốn kiểm tra các đơn hàng có thể hủy' },
  { label: '👤 Gặp nhân viên hỗ trợ', text: 'Tôi cần gặp nhân viên quản trị viên hỗ trợ trực tiếp' },
]

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
  const [aiTyping, setAiTyping] = useState(false)
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
    setAiTyping(false)
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
    connection.on('aiTyping', (event: AiTypingEvent) => {
      if (event.conversationId === selectedIdRef.current)
        setAiTyping(event.isTyping)
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
  }, [accessToken, joinConversation, loadConversations, loadMessages, markAsRead, profile.id])

  useEffect(() => {
    messageEndRef.current?.scrollIntoView({ behavior: 'smooth', block: 'nearest' })
  }, [messages, aiTyping])

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
      await loadMessages(conversation.id)
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Không thể tạo yêu cầu hỗ trợ.')
    } finally {
      setCreating(false)
    }
  }

  async function submitMessageContent(content: string) {
    if (!selectedId || selected?.status === 'Closed' || !content.trim()) return

    setSending(true)
    setError('')
    try {
      const message = await customerChatApi.sendMessage(accessToken, selectedId, content.trim())
      setMessages(items => items.some(item => item.id === message.id) ? items : [...items, message])
      await loadMessages(selectedId)
      await loadConversations()
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Không thể gửi tin nhắn.')
    } finally {
      setSending(false)
    }
  }

  async function sendMessage(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const form = event.currentTarget
    const data = new FormData(form)
    const content = String(data.get('message') ?? '').trim()
    if (!content) return
    form.reset()
    await submitMessageContent(content)
  }

  return <section className="customer-support" aria-labelledby="customer-support-title">
    <header className="customer-support__heading">
      <div><p>TRỢ LÝ AI & HỖ TRỢ KHÁCH HÀNG</p><h2 id="customer-support-title">Hỗ trợ trực tuyến & Tra cứu AI 24/7</h2><span>Trợ lý AI TransFlow tự động tra cứu vận đơn, báo giá tức thì hoặc kết nối Quản trị viên.</span></div>
      <div className={`chat-connection chat-connection--${connectionState}`}><i/>{connectionState === 'online' ? 'Trực tuyến' : connectionState === 'connecting' ? 'Đang kết nối lại' : 'Tạm mất kết nối'}</div>
    </header>

    {error && <div className="chat-error" role="alert">{error}<button type="button" onClick={() => setError('')}>×</button></div>}

    <div className="customer-support__layout">
      <aside className="chat-sidebar">
        <div className="chat-sidebar__top"><div><b>Cuộc trò chuyện</b>{totalUnread > 0 && <span>{totalUnread}</span>}</div><button type="button" onClick={() => setShowCreate(value => !value)}>{showCreate ? 'Đóng' : '+ Tạo mới'}</button></div>
        {showCreate && <form className="chat-create" onSubmit={createConversation}>
          <label>Chủ đề<input name="subject" maxLength={200} required placeholder="Ví dụ: Cần tra cứu đơn hàng"/></label>
          <label>Nội dung<textarea name="initialMessage" maxLength={2000} required rows={4} placeholder="Nhập mã đơn (ORD-...) hoặc câu hỏi để Trợ lý AI giải đáp ngay..."/></label>
          <button disabled={creating}>{creating ? 'Đang gửi...' : 'Bắt đầu trò chuyện'}</button>
        </form>}
        <div className="chat-conversation-list">
          {loading ? <div className="chat-list-state">Đang tải...</div> : conversations.length === 0 ? <div className="chat-list-state"><b>Chưa có cuộc trò chuyện</b><span>Hãy tạo yêu cầu để Trợ lý AI hoặc Quản trị viên hỗ trợ bạn.</span></div> : conversations.map(item => <button type="button" className={item.id === selectedId ? 'active' : ''} onClick={() => void selectConversation(item.id)} key={item.id}>
            <span className="chat-avatar">{item.assignedAdminName ? 'TF' : '✦'}</span><span className="chat-conversation-copy"><b>{item.subject}</b><small>{item.assignedAdminName ? `Hỗ trợ bởi ${item.assignedAdminName}` : '✦ Trợ lý AI TransFlow đang hỗ trợ'}</small><em>{formatTime(item.lastMessageAtUtc || item.createdAtUtc)}</em></span>{item.unreadCount > 0 && <i>{item.unreadCount}</i>}
          </button>)}
        </div>
      </aside>

      <div className="chat-window">
        {!selected ? <div className="chat-window__empty"><span>✦</span><h3>Trợ lý AI TransFlow luôn sẵn sàng</h3><p>Chọn một cuộc trò chuyện hoặc bấm "+ Tạo mới" để tra cứu đơn hàng, báo giá và hỗ trợ tức thì.</p></div> : <>
          <header className="chat-window__header"><div><span>{selected.assignedAdminName ? 'TF' : '✦'}</span><div><b>{selected.subject}</b><small>{selected.assignedAdminName ? `Nhân viên phụ trách: ${selected.assignedAdminName}` : '✦ Trợ lý AI TransFlow trả lời tự động · Quản trị viên có thể tiếp nhận bất cứ lúc nào'}</small></div></div><em className={`chat-status chat-status--${selected.status.toLowerCase()}`}>{statusLabels[selected.status]}</em></header>
          <div className="chat-messages" aria-live="polite">
            {messagesLoading ? <div className="chat-window__empty compact">Đang tải tin nhắn...</div> : messages.length === 0 ? <div className="chat-window__empty compact">Chưa có tin nhắn.</div> : messages.map(message => {
              const own = message.senderUserId === profile.id
              const isAi = message.messageType === 'AiAssistant'
              return <article className={`chat-message ${own ? 'chat-message--own' : isAi ? 'chat-message--ai' : ''}`} key={message.id}><div><small>{own ? 'Bạn' : isAi ? `✦ ${message.senderName}` : message.senderName}</small><p>{message.content}</p><footer><time>{formatTime(message.sentAtUtc)}</time>{own && <span>{message.readAtUtc ? 'Đã đọc' : 'Đã gửi'}</span>}</footer></div></article>
            })}
            {aiTyping && <div className="chat-ai-typing"><span>✦</span>Trợ lý AI TransFlow đang phân tích và phản hồi...</div>}
            <div ref={messageEndRef}/>
          </div>
          {selected.status === 'Closed' ? <div className="chat-closed">Cuộc trò chuyện đã được đóng. Vui lòng tạo yêu cầu mới nếu bạn cần thêm hỗ trợ.</div> : <>
            {selected.status === 'Open' && <div className="chat-quick-prompts" aria-label="Gợi ý câu hỏi nhanh">
              {quickPrompts.map(item => <button type="button" key={item.label} disabled={sending} onClick={() => void submitMessageContent(item.text)}>{item.label}</button>)}
            </div>}
            <form className="chat-composer" onSubmit={sendMessage}><textarea name="message" maxLength={2000} rows={2} required placeholder={selected.status === 'Open' ? 'Nhập mã đơn (VD: ORD-...) hoặc câu hỏi cho Trợ lý AI...' : 'Nhập tin nhắn gửi Quản trị viên...'} aria-label="Nội dung tin nhắn"/><button disabled={sending}>{sending ? 'Đang gửi' : 'Gửi'} <span>→</span></button></form>
          </>}
        </>}
      </div>
    </div>
  </section>
}
