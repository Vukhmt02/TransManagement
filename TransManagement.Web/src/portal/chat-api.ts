import { authFetch } from '../auth'
import type { ApiResponse, ProblemDetails } from '../types'
import type { ChatConversation, ChatMessage } from './chat-types'

async function request<T>(token: string, path: string, init: RequestInit = {}): Promise<T> {
  const response = await authFetch(path, init, token)
  if (!response.ok) {
    const problem = (await response.json().catch(() => ({}))) as ProblemDetails
    throw new Error(problem.detail || `Không thể xử lý yêu cầu (${response.status}).`)
  }
  if (response.status === 204) return undefined as T
  return ((await response.json()) as ApiResponse<T>).data
}

const json = (method: string, body?: unknown): RequestInit => ({
  method,
  headers: body === undefined ? undefined : { 'Content-Type': 'application/json' },
  body: body === undefined ? undefined : JSON.stringify(body),
})

export const customerChatApi = {
  conversations: (token: string) =>
    request<ChatConversation[]>(token, '/api/chat/conversations'),
  messages: (token: string, conversationId: string) =>
    request<ChatMessage[]>(token, `/api/chat/conversations/${conversationId}/messages`),
  createConversation: (token: string, subject: string, initialMessage: string) =>
    request<ChatConversation>(token, '/api/chat/conversations', json('POST', { subject, initialMessage })),
  sendMessage: (token: string, conversationId: string, content: string) =>
    request<ChatMessage>(token, `/api/chat/conversations/${conversationId}/messages`, json('POST', { content })),
  markAsRead: (token: string, conversationId: string) =>
    request<void>(token, `/api/chat/conversations/${conversationId}/read`, json('PUT')),
}

export const adminChatApi = {
  conversations: (token: string) =>
    request<ChatConversation[]>(token, '/api/chat/conversations'),
  messages: (token: string, conversationId: string) =>
    request<ChatMessage[]>(token, `/api/chat/conversations/${conversationId}/messages`),
  sendMessage: (token: string, conversationId: string, content: string) =>
    request<ChatMessage>(token, `/api/chat/conversations/${conversationId}/messages`, json('POST', { content })),
  markAsRead: (token: string, conversationId: string) =>
    request<void>(token, `/api/chat/conversations/${conversationId}/read`, json('PUT')),
  assign: (token: string, conversationId: string) =>
    request<ChatConversation>(token, `/api/chat/conversations/${conversationId}/assign`, json('PUT')),
  close: (token: string, conversationId: string) =>
    request<void>(token, `/api/chat/conversations/${conversationId}/close`, json('PUT')),
  reopen: (token: string, conversationId: string) =>
    request<void>(token, `/api/chat/conversations/${conversationId}/reopen`, json('PUT')),
}
