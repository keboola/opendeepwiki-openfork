"use client";

import { useState, useEffect } from "react";
import { useRouter } from "next/navigation";
import { AppLayout } from "@/components/app-layout";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Switch } from "@/components/ui/switch";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { useTranslations } from "@/hooks/use-translations";
import { useAuth } from "@/contexts/auth-context";
import { getUserSettings, updateUserSettings, UserSettings, getSystemVersion, SystemVersion } from "@/lib/profile-api";
import { Loader2, Settings, Bell, Globe, Palette, ArrowLeft } from "lucide-react";
import { toast } from "sonner";
import Link from "next/link";
import { useTheme } from "next-themes";

export default function SettingsPage() {
  const t = useTranslations();
  const router = useRouter();
  const { isLoading: authLoading, isAuthenticated } = useAuth();
  const { theme, setTheme } = useTheme();
  const [activeItem, setActiveItem] = useState(t("common.settings.title"));

  const [settings, setSettings] = useState<UserSettings>({
    theme: "system",
    language: "en",
    emailNotifications: true,
    pushNotifications: false,
  });
  const [systemVersion, setSystemVersion] = useState<SystemVersion | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);

  useEffect(() => {
    if (!authLoading && !isAuthenticated) {
      router.push("/auth?returnUrl=/settings");
    }
  }, [authLoading, isAuthenticated, router]);

  useEffect(() => {
    if (isAuthenticated) {
      loadSettings();
    }
  }, [isAuthenticated]);

  useEffect(() => {
    loadSystemVersion();
  }, []);

  const loadSettings = async () => {
    try {
      const data = await getUserSettings();
      setSettings(data);
    } catch {
      // Use default settings
    } finally {
      setIsLoading(false);
    }
  };

  const loadSystemVersion = async () => {
    try {
      const data = await getSystemVersion();
      setSystemVersion(data);
    } catch {
      // Use default version
    }
  };

  const handleSave = async () => {
    setIsSaving(true);
    try {
      await updateUserSettings(settings);
      toast.success(t("settings.saveSuccess") || "Settings saved");
    } catch (err) {
      toast.error(err instanceof Error ? err.message : t("settings.saveFailed") || "Save failed");
    } finally {
      setIsSaving(false);
    }
  };

  const handleThemeChange = (value: string) => {
    setSettings((prev) => ({ ...prev, theme: value as UserSettings["theme"] }));
    setTheme(value);
  };

  const handleLanguageChange = (value: string) => {
    setSettings((prev) => ({ ...prev, language: value }));
    // Language switching is handled by LanguageToggle component, only save settings here
  };

  if (authLoading || isLoading) {
    return (
      <div className="flex h-screen items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  if (!isAuthenticated) {
    return null;
  }

  return (
    <AppLayout activeItem={activeItem} onItemClick={setActiveItem}>
      <div className="flex flex-1 flex-col p-4 md:p-6 max-w-4xl mx-auto w-full">
        <div className="mb-6">
          <Link
            href="/"
            className="inline-flex items-center gap-2 text-sm text-muted-foreground hover:text-foreground transition-colors"
          >
            <ArrowLeft className="h-4 w-4" />
            {t("common.backToHome") || "Back to Home"}
          </Link>
        </div>

        <div className="space-y-6">
          {/* Appearance Settings */}
          <Card>
            <CardHeader>
              <div className="flex items-center gap-2">
                <Palette className="h-5 w-5" />
                <CardTitle>{t("settings.appearance") || "Appearance"}</CardTitle>
              </div>
              <CardDescription>
                {t("settings.appearanceDescription") || "Customize the look and feel of the application"}
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-6">
              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label>{t("settings.theme") || "Theme"}</Label>
                  <p className="text-sm text-muted-foreground">
                    {t("settings.themeDescription") || "Choose the color theme for the application"}
                  </p>
                </div>
                <Select value={theme || settings.theme} onValueChange={handleThemeChange}>
                  <SelectTrigger className="w-32">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="light">{t("settings.themeLight") || "Light"}</SelectItem>
                    <SelectItem value="dark">{t("settings.themeDark") || "Dark"}</SelectItem>
                    <SelectItem value="system">{t("settings.themeSystem") || "System"}</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label>{t("settings.language") || "Language"}</Label>
                  <p className="text-sm text-muted-foreground">
                    {t("settings.languageDescription") || "Choose the display language"}
                  </p>
                </div>
                <Select value={settings.language} onValueChange={handleLanguageChange}>
                  <SelectTrigger className="w-32">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="zh">中文</SelectItem>
                    <SelectItem value="en">English</SelectItem>
                    <SelectItem value="ja">日本語</SelectItem>
                    <SelectItem value="ko">한국어</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </CardContent>
          </Card>

          {/* Notification Settings */}
          <Card>
            <CardHeader>
              <div className="flex items-center gap-2">
                <Bell className="h-5 w-5" />
                <CardTitle>{t("settings.notifications") || "Notifications"}</CardTitle>
              </div>
              <CardDescription>
                {t("settings.notificationsDescription") || "Manage your notification preferences"}
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-6">
              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label>{t("settings.emailNotifications") || "Email Notifications"}</Label>
                  <p className="text-sm text-muted-foreground">
                    {t("settings.emailNotificationsDescription") || "Receive email notifications for important updates"}
                  </p>
                </div>
                <Switch
                  checked={settings.emailNotifications}
                  onCheckedChange={(checked) =>
                    setSettings((prev) => ({ ...prev, emailNotifications: checked }))
                  }
                />
              </div>

              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label>{t("settings.pushNotifications") || "Push Notifications"}</Label>
                  <p className="text-sm text-muted-foreground">
                    {t("settings.pushNotificationsDescription") || "Receive browser push notifications"}
                  </p>
                </div>
                <Switch
                  checked={settings.pushNotifications}
                  onCheckedChange={(checked) =>
                    setSettings((prev) => ({ ...prev, pushNotifications: checked }))
                  }
                />
              </div>
            </CardContent>
          </Card>

          {/* About */}
          <Card>
            <CardHeader>
              <div className="flex items-center gap-2">
                <Globe className="h-5 w-5" />
                <CardTitle>{t("settings.about") || "About"}</CardTitle>
              </div>
            </CardHeader>
            <CardContent>
              <div className="space-y-2 text-sm text-muted-foreground">
                <p>{systemVersion?.productName || "KeboolaDeepWiki"} v{systemVersion?.version || "1.0.0"}</p>
                <p>{t("settings.aboutDescription") || "AI-powered code knowledge base platform"}</p>
              </div>
            </CardContent>
          </Card>

          {/* Save Button */}
          <div className="flex justify-end">
            <Button onClick={handleSave} disabled={isSaving}>
              {isSaving ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  {t("common.loading") || "Saving..."}
                </>
              ) : (
                t("common.save") || "Save Settings"
              )}
            </Button>
          </div>
        </div>
      </div>
    </AppLayout>
  );
}
