"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Mail, BookOpen, Sparkles, Zap, ArrowLeft, Loader2 } from "lucide-react";
import { useRouter, useSearchParams } from "next/navigation";
import Link from "next/link";
import { useTranslations } from "@/hooks/use-translations";

export default function AuthPage() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const returnUrl = searchParams.get("returnUrl");
  const t = useTranslations();
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState("");

  const handleOAuthLogin = async (provider: string) => {
    setError("");
    setIsLoading(true);
    try {
      const response = await fetch(`/api/oauth/${provider}/authorize`);
      const result = await response.json();
      if (result.success && result.data?.authorizationUrl) {
        const authUrl = new URL(result.data.authorizationUrl);
        if (returnUrl) {
          authUrl.searchParams.set("state", encodeURIComponent(returnUrl));
        }
        window.location.href = authUrl.toString();
      } else {
        setError(result.message || "Failed to start login");
        setIsLoading(false);
      }
    } catch {
      setError("Failed to start login");
      setIsLoading(false);
    }
  };

  return (
    <div className="flex min-h-screen">
      {/* Left Side - Brand Section */}
      <div className="hidden lg:flex lg:w-1/2 bg-gradient-to-br from-primary/10 via-primary/5 to-background relative overflow-hidden">
        <div className="absolute inset-0 bg-grid-white/5 [mask-image:radial-gradient(white,transparent_85%)]" />

        <div className="relative z-10 flex flex-col justify-between p-12 w-full">
          <div>
            <Link
              href="/"
              className="inline-flex items-center gap-2 text-sm text-muted-foreground hover:text-foreground transition-colors"
            >
              <ArrowLeft className="h-4 w-4" />
              {t("authUi.backToHome")}
            </Link>
          </div>

          <div className="space-y-8">
            <div className="space-y-4">
              <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-primary/10 text-primary text-sm font-medium">
                <Sparkles className="h-4 w-4" />
                {t("authUi.aiPowered")}
              </div>
              <h1 className="text-5xl font-bold tracking-tight">{t("authUi.title")}</h1>
              <p className="text-xl text-muted-foreground max-w-md">
                {t("authUi.subtitle")}
              </p>
            </div>

            <div className="space-y-6 max-w-md">
              <div className="flex items-start gap-4">
                <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-primary/10">
                  <BookOpen className="h-5 w-5 text-primary" />
                </div>
                <div>
                  <h3 className="font-semibold mb-1">{t("authUi.features.smartDocs")}</h3>
                  <p className="text-sm text-muted-foreground">
                    {t("authUi.features.smartDocsDesc")}
                  </p>
                </div>
              </div>

              <div className="flex items-start gap-4">
                <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-primary/10">
                  <Zap className="h-5 w-5 text-primary" />
                </div>
                <div>
                  <h3 className="font-semibold mb-1">{t("authUi.fastSearch")}</h3>
                  <p className="text-sm text-muted-foreground">
                    {t("authUi.features.fastSearchDesc")}
                  </p>
                </div>
              </div>
            </div>
          </div>

          <div className="text-sm text-muted-foreground">
            {t("authUi.copyright")}
          </div>
        </div>
      </div>

      {/* Right Side - Google Sign In */}
      <div className="flex-1 flex items-center justify-center p-8 bg-background">
        <div className="w-full max-w-md space-y-8">
          {/* Mobile Header */}
          <div className="lg:hidden text-center space-y-2">
            <h1 className="text-3xl font-bold">{t("authUi.title")}</h1>
            <p className="text-muted-foreground">{t("authUi.mobileSubtitle")}</p>
          </div>

          <div className="space-y-6">
            <div className="space-y-2 text-center lg:text-left">
              <h2 className="text-2xl font-bold tracking-tight">
                {t("authUi.welcome")}
              </h2>
              <p className="text-muted-foreground">
                Sign in with your Google Workspace account to continue.
              </p>
            </div>

            {error && (
              <div className="p-3 text-sm text-red-500 bg-red-50 dark:bg-red-950/50 rounded-md">
                {error}
              </div>
            )}

            <Button
              variant="outline"
              onClick={() => handleOAuthLogin("google")}
              disabled={isLoading}
              className="w-full gap-2 h-12 text-base"
            >
              {isLoading ? (
                <Loader2 className="h-5 w-5 animate-spin" />
              ) : (
                <Mail className="h-5 w-5" />
              )}
              {isLoading ? "Redirecting..." : "Sign in with Google"}
            </Button>

            <div className="lg:hidden text-center">
              <Button variant="ghost" onClick={() => router.push("/")} className="gap-2">
                <ArrowLeft className="h-4 w-4" />
                {t("authUi.backToHome")}
              </Button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
