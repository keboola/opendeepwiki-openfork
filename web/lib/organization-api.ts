import { getToken } from "./auth-api";
import { getApiProxyUrl } from "./env";

const API_BASE_URL = getApiProxyUrl();

function buildApiUrl(path: string) {
  if (!API_BASE_URL) {
    return path;
  }
  const trimmedBase = API_BASE_URL.endsWith("/") ? API_BASE_URL.slice(0, -1) : API_BASE_URL;
  return `${trimmedBase}${path}`;
}

async function fetchWithAuth(url: string, options: RequestInit = {}) {
  const token = getToken();
  const headers: HeadersInit = {
    "Content-Type": "application/json",
    ...(options.headers || {}),
  };

  if (token) {
    (headers as Record<string, string>)["Authorization"] = `Bearer ${token}`;
  }

  const response = await fetch(url, { ...options, headers });

  if (!response.ok) {
    const error = await response.json().catch(() => ({}));
    throw new Error(error.message || `Request failed: ${response.status}`);
  }

  return response.json();
}

export interface UserDepartment {
  id: string;
  name: string;
  description?: string;
  isManager: boolean;
}

export interface DepartmentRepository {
  repositoryId: string;
  repoName: string;
  orgName: string;
  gitUrl?: string;
  status: number;
  statusName: string;
  departmentId: string;
  departmentName: string;
  createdAt?: string;
  primaryLanguage?: string;
  isRestricted?: boolean;
}

/**
 * Get the current user's department list
 */
export async function getMyDepartments(): Promise<UserDepartment[]> {
  const url = buildApiUrl("/api/organizations/my-departments");
  const result = await fetchWithAuth(url);
  return result.data;
}

/**
 * Get the repository list under the current user's departments
 */
export async function getMyDepartmentRepositories(includeRestricted = false): Promise<DepartmentRepository[]> {
  const params = includeRestricted ? '?includeRestricted=true' : '';
  const url = buildApiUrl(`/api/organizations/my-repositories${params}`);
  const result = await fetchWithAuth(url);
  return result.data;
}

export async function shareRepoWithOrganization(repositoryId: string): Promise<{ success: boolean }> {
  const url = buildApiUrl(`/api/organizations/my-repositories/${repositoryId}/share`);
  return fetchWithAuth(url, { method: "POST" });
}

export async function unshareRepoFromOrganization(repositoryId: string): Promise<{ success: boolean }> {
  const url = buildApiUrl(`/api/organizations/my-repositories/${repositoryId}/share`);
  return fetchWithAuth(url, { method: "DELETE" });
}

export async function restrictRepoInOrganization(repositoryId: string): Promise<{ success: boolean }> {
  const url = buildApiUrl(`/api/organizations/repositories/${repositoryId}/restrict`);
  const response = await fetch(url, {
    method: "POST",
    credentials: "include",
  });
  if (!response.ok) throw new Error("Failed to restrict repository");
  return response.json();
}

export async function unrestrictRepoInOrganization(repositoryId: string): Promise<{ success: boolean }> {
  const url = buildApiUrl(`/api/organizations/repositories/${repositoryId}/unrestrict`);
  const response = await fetch(url, {
    method: "POST",
    credentials: "include",
  });
  if (!response.ok) throw new Error("Failed to unrestrict repository");
  return response.json();
}
