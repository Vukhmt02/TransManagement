import { HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr'
import { getStoredTokens } from '../auth'

export function createChatConnection(fallbackToken: string) {
  return new HubConnectionBuilder()
    .withUrl('/hubs/chat', {
      accessTokenFactory: () => getStoredTokens()?.accessToken ?? fallbackToken,
    })
    .withAutomaticReconnect([0, 2_000, 5_000, 10_000])
    .configureLogging(LogLevel.Warning)
    .build()
}

export { HubConnectionState }
