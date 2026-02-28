import { cookies } from "next/headers";

const TOKEN_KEY = "auth_token";

/**
 * Get authorization headers from the server-side cookie store.
 * Used in server components to forward the JWT to backend API calls.
 */
export async function getServerAuthHeaders(): Promise<HeadersInit> {
  const cookieStore = await cookies();
  const token = cookieStore.get(TOKEN_KEY)?.value;
  if (token) {
    return { Authorization: `Bearer ${token}` };
  }
  return {};
}
