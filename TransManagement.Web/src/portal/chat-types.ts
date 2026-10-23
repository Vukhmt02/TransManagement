export type ConversationStatus = 'Open' | 'Assigned' | 'Closed'

export interface ChatConversation {
  id: string
  customerUserId: string
  customerName: string
  assignedAdminUserId?: string
  assignedAdminName?: string
  subject: string
  status: ConversationStatus
  createdAtUtc: string
  updatedAtUtc?: string
  closedAtUtc?: string
  lastMessageAtUtc?: string
  unreadCount: number
}

export interface ChatMessage {
  id: string
  conversationId: string
  senderUserId: string
  senderName: string
  content: string
  sentAtUtc: string
  readAtUtc?: string
  messageType: 'Text'
}

export interface ConversationChangedEvent {
  conversationId: string
  status: ConversationStatus
  customerUserId: string
  assignedAdminUserId?: string
  change: 'created' | 'messageSent' | 'assigned' | 'closed' | 'reopened'
  occurredAtUtc: string
}

export interface MessagesReadEvent {
  conversationId: string
  readByUserId: string
  readAtUtc: string
}
