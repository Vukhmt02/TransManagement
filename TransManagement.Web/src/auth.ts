import type { ApiResponse, AuthTokens, ProblemDetails, UserProfile } from './types'

const TOKEN_KEY = 'transmanagement.auth'
let refreshInFlight: Promise<AuthTokens | null> | null = null

function storeTokens(tokens: AuthTokens) {
  sessionStorage.setItem(TOKEN_KEY, JSON.stringify(tokens))
}

export async function login(email: string, password: string): Promise<AuthTokens> {
  const response = await fetch('/api/Auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password }),
  })
  if (!response.ok) {
    const problem = (await response.json().catch(() => ({}))) as ProblemDetails
    throw new Error(problem.detail || 'Email hoặc mật khẩu không chính xác.')
  }
  const result = (await response.json()) as ApiResponse<AuthTokens>
  storeTokens(result.data)
  return result.data
}

export async function register(fullName: string, phone: string, email: string, password: string): Promise<AuthTokens> {
  const response = await fetch('/api/Auth/register', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ fullName, phone, email, password }),
  })
  if (!response.ok) {
    const problem = (await response.json().catch(() => ({}))) as ProblemDetails
    throw new Error(problem.detail || 'Không thể tạo tài khoản. Email có thể đã được sử dụng.')
  }
  const result = (await response.json()) as ApiResponse<AuthTokens>
  storeTokens(result.data)
  return result.data
}

export async function getProfile(accessToken: string): Promise<UserProfile> {
  const response = await authFetch('/api/Auth/me', {}, accessToken)
  if (!response.ok) throw new Error('Không thể tải thông tin tài khoản.')
  const result = (await response.json()) as ApiResponse<UserProfile>
  return result.data
}

export async function logout(tokens: AuthTokens | null): Promise<void> {
  const currentTokens = getStoredTokens() ?? tokens
  if (currentTokens?.refreshToken) {
    await fetch('/api/Auth/logout', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken: currentTokens.refreshToken }),
    }).catch(() => undefined)
  }
  sessionStorage.removeItem(TOKEN_KEY)
}

async function renewTokens(): Promise<AuthTokens | null> {
  if (!refreshInFlight) {
    refreshInFlight = (async () => {
      const current = getStoredTokens()
      if (!current?.refreshToken) return null
      const response = await fetch('/api/Auth/refresh-token', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ refreshToken: current.refreshToken }),
      })
      if (!response.ok) {
        sessionStorage.removeItem(TOKEN_KEY)
        return null
      }
      const result = (await response.json()) as ApiResponse<AuthTokens>
      storeTokens(result.data)
      return result.data
    })()
  }
  try { return await refreshInFlight }
  finally { refreshInFlight = null }
}

export async function authFetch(input: RequestInfo | URL, init: RequestInit = {}, fallbackToken?: string): Promise<Response> {
  const current = getStoredTokens()
  const headers = new Headers(init.headers)
  headers.set('Authorization', `Bearer ${current?.accessToken ?? fallbackToken ?? ''}`)
  let response = await fetch(input, { ...init, headers })
  if (response.status !== 401) return response

  const renewed = await renewTokens()
  if (!renewed) return response
  headers.set('Authorization', `Bearer ${renewed.accessToken}`)
  response = await fetch(input, { ...init, headers })
  return response
}

export function getStoredTokens(): AuthTokens | null {
  try {
    const value = sessionStorage.getItem(TOKEN_KEY)
    return value ? (JSON.parse(value) as AuthTokens) : null
  } catch {
    sessionStorage.removeItem(TOKEN_KEY)
    return null
  }
}
