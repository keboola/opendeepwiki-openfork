/**
 * Recommendation system API client
 */

import { api } from "./api-client";

/** Recommended repository item */
export interface RecommendedRepository {
  id: string;
  repoName: string;
  orgName: string;
  primaryLanguage?: string;
  starCount: number;
  forkCount: number;
  bookmarkCount: number;
  subscriptionCount: number;
  viewCount: number;
  createdAt: string;
  updatedAt?: string;
  score: number;
  scoreBreakdown?: ScoreBreakdown;
  recommendReason?: string;
}

/** Score breakdown */
export interface ScoreBreakdown {
  popularity: number;
  subscription: number;
  timeDecay: number;
  userPreference: number;
  privateRepoLanguage: number;
  collaborative: number;
}

/** Recommendation response */
export interface RecommendationResponse {
  items: RecommendedRepository[];
  strategy: string;
  totalCandidates: number;
}

/** Recommendation request parameters */
export interface RecommendationParams {
  userId?: string;
  limit?: number;
  strategy?: "default" | "popular" | "personalized" | "explore";
  language?: string;
}

/** Record activity request */
export interface RecordActivityRequest {
  userId: string;
  repositoryId?: string;
  activityType: "View" | "Search" | "Bookmark" | "Subscribe" | "Analyze";
  duration?: number;
  searchQuery?: string;
  language?: string;
}

/** Record activity response */
export interface RecordActivityResponse {
  success: boolean;
  errorMessage?: string;
}

/** Dislike request */
export interface DislikeRequest {
  userId: string;
  repositoryId: string;
  reason?: string;
}

/** Dislike response */
export interface DislikeResponse {
  success: boolean;
  errorMessage?: string;
}

/** Language info */
export interface LanguageInfo {
  name: string;
  count: number;
}

/** Available languages list response */
export interface AvailableLanguagesResponse {
  languages: LanguageInfo[];
}

/**
 * Get recommended repository list
 */
export async function getRecommendations(
  params: RecommendationParams = {}
): Promise<RecommendationResponse> {
  const searchParams = new URLSearchParams();
  
  if (params.userId) searchParams.set("userId", params.userId);
  if (params.limit) searchParams.set("limit", params.limit.toString());
  if (params.strategy) searchParams.set("strategy", params.strategy);
  if (params.language) searchParams.set("language", params.language);

  const queryString = searchParams.toString();
  const path = `/api/v1/recommendations${queryString ? `?${queryString}` : ""}`;
  
  return api.get<RecommendationResponse>(path);
}

/**
 * Get popular repositories
 */
export async function getPopularRepos(
  limit: number = 20,
  language?: string
): Promise<RecommendationResponse> {
  const searchParams = new URLSearchParams();
  searchParams.set("limit", limit.toString());
  if (language) searchParams.set("language", language);

  return api.get<RecommendationResponse>(
    `/api/v1/recommendations/popular?${searchParams.toString()}`
  );
}

/**
 * Get available programming languages list
 */
export async function getAvailableLanguages(): Promise<AvailableLanguagesResponse> {
  return api.get<AvailableLanguagesResponse>("/api/v1/recommendations/languages");
}

/**
 * Record user activity
 */
export async function recordActivity(
  request: RecordActivityRequest
): Promise<RecordActivityResponse> {
  return api.post<RecordActivityResponse>("/api/v1/recommendations/activity", request);
}

/**
 * Mark repository as not interested
 */
export async function markAsDisliked(
  request: DislikeRequest
): Promise<DislikeResponse> {
  return api.post<DislikeResponse>("/api/v1/recommendations/dislike", request);
}

/**
 * Remove not-interested mark
 */
export async function removeDislike(
  userId: string,
  repositoryId: string
): Promise<{ success: boolean }> {
  return api.delete<{ success: boolean }>(
    `/api/v1/recommendations/dislike/${repositoryId}?userId=${userId}`
  );
}

/**
 * Refresh user preference cache
 */
export async function refreshUserPreference(
  userId: string
): Promise<{ success: boolean; message: string }> {
  return api.post<{ success: boolean; message: string }>(
    `/api/v1/recommendations/refresh-preference/${userId}`
  );
}
