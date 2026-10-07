export interface ApiResponse<T> {
  success: boolean
  data: T
  message?: string
}

export interface AuthTokens {
  accessToken: string
  refreshToken: string
  accessTokenExpiresAtUtc: string
}

export interface UserProfile {
  id: string
  email: string
  fullName: string
  roles: string[]
}

export interface ProblemDetails {
  title?: string
  detail?: string
  status?: number
  code?: string
}
