"use client";

import { useEffect, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { Loader2 } from "lucide-react";
import { setToken } from "@/lib/auth-api";

export default function OAuthCallbackPage() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const code = searchParams.get("code");
    const state = searchParams.get("state");

    if (!code) {
      setError("No authorization code received");
      return;
    }

    const provider = "google";

    const exchangeCode = async () => {
      try {
        const params = new URLSearchParams({ code });
        if (state) params.set("state", state);

        const response = await fetch(
          `/api/oauth/${provider}/callback?${params.toString()}`
        );
        const result = await response.json();

        if (!result.success || !result.data?.accessToken) {
          setError(result.message || "OAuth login failed");
          return;
        }

        setToken(result.data.accessToken);

        // State may be a return URL (starts with /) or a backend-generated UUID
        const decoded = state ? decodeURIComponent(state) : "/";
        const returnUrl = decoded.startsWith("/") ? decoded : "/";
        window.location.href = returnUrl;
      } catch {
        setError("Failed to complete OAuth login");
      }
    };

    exchangeCode();
  }, [searchParams, router]);

  if (error) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <div className="text-center space-y-4">
          <p className="text-red-500">{error}</p>
          <a href="/auth" className="text-primary underline">
            Back to login
          </a>
        </div>
      </div>
    );
  }

  return (
    <div className="flex min-h-screen items-center justify-center">
      <div className="text-center space-y-4">
        <Loader2 className="h-8 w-8 animate-spin mx-auto" />
        <p className="text-muted-foreground">Completing login...</p>
      </div>
    </div>
  );
}
