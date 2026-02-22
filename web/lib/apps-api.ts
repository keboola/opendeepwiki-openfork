/**
 * User application API client
 *
 * Implements app CRUD, statistics queries, and log query API calls
 * Requirements: 12.6, 15.6, 16.3
 */

import { api } from './api-client'

// ==================== App-related types ====================

/**
 * Create app request
 */
export interface CreateChatAppDto {
  name: string
  description?: string
  iconUrl?: string
  enableDomainValidation: boolean
  allowedDomains?: string[]
  providerType: string
  apiKey?: string
  baseUrl?: string
  availableModels?: string[]
  defaultModel?: string
  rateLimitPerMinute?: number
}

/**
 * Update app request
 */
export interface UpdateChatAppDto {
  name?: string
  description?: string
  iconUrl?: string
  enableDomainValidation?: boolean
  allowedDomains?: string[]
  providerType?: string
  apiKey?: string
  baseUrl?: string
  availableModels?: string[]
  defaultModel?: string
  rateLimitPerMinute?: number
  isActive?: boolean
}

/**
 * App response
 */
export interface ChatAppDto {
  id: string
  userId: string
  name: string
  description?: string
  iconUrl?: string
  appId: string
  appSecret?: string
  enableDomainValidation: boolean
  allowedDomains: string[]
  providerType: string
  apiKey?: string
  baseUrl?: string
  availableModels: string[]
  defaultModel?: string
  rateLimitPerMinute?: number
  isActive: boolean
  createdAt: string
  updatedAt?: string
}

// ==================== Statistics-related types ====================

/**
 * Daily statistics data
 */
export interface AppStatisticsDto {
  appId: string
  date: string
  requestCount: number
  inputTokens: number
  outputTokens: number
}

/**
 * Aggregated statistics data
 */
export interface AggregatedStatisticsDto {
  appId: string
  startDate: string
  endDate: string
  totalRequests: number
  totalInputTokens: number
  totalOutputTokens: number
  dailyStatistics: AppStatisticsDto[]
}

// ==================== Chat log related types ====================

/**
 * Chat log entry
 */
export interface ChatLogDto {
  id: string
  appId: string
  userIdentifier?: string
  question: string
  answerSummary?: string
  inputTokens: number
  outputTokens: number
  modelUsed?: string
  sourceDomain?: string
  createdAt: string
}

/**
 * Paginated chat logs response
 */
export interface PaginatedChatLogsDto {
  items: ChatLogDto[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

/**
 * Chat log query parameters
 */
export interface ChatLogQueryParams {
  startDate?: string
  endDate?: string
  keyword?: string
  page?: number
  pageSize?: number
}

// ==================== App CRUD API ====================

/**
 * Get the current user's app list
 */
export async function getUserApps(): Promise<ChatAppDto[]> {
  return api.get<ChatAppDto[]>('/api/v1/apps')
}

/**
 * Create a new app
 */
export async function createApp(dto: CreateChatAppDto): Promise<ChatAppDto> {
  return api.post<ChatAppDto>('/api/v1/apps', dto)
}

/**
 * Get app details
 */
export async function getAppById(id: string): Promise<ChatAppDto> {
  return api.get<ChatAppDto>(`/api/v1/apps/${id}`)
}

/**
 * Update app
 */
export async function updateApp(id: string, dto: UpdateChatAppDto): Promise<ChatAppDto> {
  return api.put<ChatAppDto>(`/api/v1/apps/${id}`, dto)
}

/**
 * Delete app
 */
export async function deleteApp(id: string): Promise<void> {
  return api.delete<void>(`/api/v1/apps/${id}`)
}

/**
 * Regenerate app secret
 */
export async function regenerateAppSecret(id: string): Promise<{ appSecret: string }> {
  return api.post<{ appSecret: string }>(`/api/v1/apps/${id}/regenerate-secret`)
}

// ==================== Statistics API ====================

/**
 * Get app statistics data
 */
export async function getAppStatistics(
  id: string,
  startDate?: string,
  endDate?: string
): Promise<AggregatedStatisticsDto> {
  const params = new URLSearchParams()
  if (startDate) params.append('startDate', startDate)
  if (endDate) params.append('endDate', endDate)
  
  const queryString = params.toString()
  const url = `/api/v1/apps/${id}/statistics${queryString ? `?${queryString}` : ''}`
  
  return api.get<AggregatedStatisticsDto>(url)
}

// ==================== Chat Log API ====================

/**
 * Get app chat logs
 */
export async function getAppLogs(
  id: string,
  params?: ChatLogQueryParams
): Promise<PaginatedChatLogsDto> {
  const searchParams = new URLSearchParams()
  
  if (params?.startDate) searchParams.append('startDate', params.startDate)
  if (params?.endDate) searchParams.append('endDate', params.endDate)
  if (params?.keyword) searchParams.append('keyword', params.keyword)
  if (params?.page) searchParams.append('page', params.page.toString())
  if (params?.pageSize) searchParams.append('pageSize', params.pageSize.toString())
  
  const queryString = searchParams.toString()
  const url = `/api/v1/apps/${id}/logs${queryString ? `?${queryString}` : ''}`
  
  return api.get<PaginatedChatLogsDto>(url)
}

// ==================== Helper functions ====================

/**
 * Model provider types
 */
export const PROVIDER_TYPES = [
  { value: 'OpenAI', label: 'OpenAI' },
  { value: 'OpenAIResponses', label: 'OpenAI Responses' },
  { value: 'Anthropic', label: 'Anthropic' },
] as const

export type ProviderType = typeof PROVIDER_TYPES[number]['value']

/**
 * Format date as ISO string (date part only)
 */
export function formatDateForApi(date: Date): string {
  return date.toISOString().split('T')[0]
}

/**
 * Get default date range (last 30 days)
 */
export function getDefaultDateRange(): { startDate: string; endDate: string } {
  const endDate = new Date()
  const startDate = new Date()
  startDate.setDate(startDate.getDate() - 30)
  
  return {
    startDate: formatDateForApi(startDate),
    endDate: formatDateForApi(endDate),
  }
}
