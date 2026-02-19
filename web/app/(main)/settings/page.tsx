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
      // 使用默认设置
    } finally {
      setIsLoading(false);
    }
  };

  const loadSystemVersion = async () => {
    try {
      const data = await getSystemVersion();
      setSystemVersion(data);
    } catch {
      // 使用默认版本
    }
  };

  const handleSave = async () => {
    setIsSaving(true);
    try {
      await updateUserSettings(settings);
      toast.success(t("settings.saveSuccess") || "设置已保存");
    } catch (err) {
      toast.error(err instanceof Error ? err.message : t("settings.saveFailed") || "保存失败");
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
    // 语言切换由 LanguageToggle 组件处理，这里只保存设置
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
            {t("common.backToHome") || "返回首页"}
          </Link>
        </div>

        <div className="space-y-6">
          {/* Appearance Settings */}
          <Card>
            <CardHeader>
              <div className="flex items-center gap-2">
                <Palette className="h-5 w-5" />
                <CardTitle>{t("settings.appearance") || "外观设置"}</CardTitle>
              </div>
              <CardDescription>
                {t("settings.appearanceDescription") || "自定义应用的外观和显示"}
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-6">
              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label>{t("settings.theme") || "主题"}</Label>
                  <p className="text-sm text-muted-foreground">
                    {t("settings.themeDescription") || "选择应用的颜色主题"}
                  </p>
                </div>
                <Select value={theme || settings.theme} onValueChange={handleThemeChange}>
                  <SelectTrigger className="w-32">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="light">{t("settings.themeLight") || "浅色"}</SelectItem>
                    <SelectItem value="dark">{t("settings.themeDark") || "深色"}</SelectItem>
                    <SelectItem value="system">{t("settings.themeSystem") || "跟随系统"}</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label>{t("settings.language") || "语言"}</Label>
                  <p className="text-sm text-muted-foreground">
                    {t("settings.languageDescription") || "选择界面显示语言"}
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
                <CardTitle>{t("settings.notifications") || "通知设置"}</CardTitle>
              </div>
              <CardDescription>
                {t("settings.notificationsDescription") || "管理你的通知偏好"}
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-6">
              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label>{t("settings.emailNotifications") || "邮件通知"}</Label>
                  <p className="text-sm text-muted-foreground">
                    {t("settings.emailNotificationsDescription") || "接收重要更新的邮件通知"}
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
                  <Label>{t("settings.pushNotifications") || "推送通知"}</Label>
                  <p className="text-sm text-muted-foreground">
                    {t("settings.pushNotificationsDescription") || "接收浏览器推送通知"}
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
                <CardTitle>{t("settings.about") || "关于"}</CardTitle>
              </div>
            </CardHeader>
            <CardContent>
              <div className="space-y-2 text-sm text-muted-foreground">
                <p>{systemVersion?.productName || "OpenDeepWiki"} v{systemVersion?.version || "1.0.0"}</p>
                <p>{t("settings.aboutDescription") || "AI 驱动的代码知识库平台"}</p>
              </div>
            </CardContent>
          </Card>

          {/* Save Button */}
          <div className="flex justify-end">
            <Button onClick={handleSave} disabled={isSaving}>
              {isSaving ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  {t("common.loading") || "保存中..."}
                </>
              ) : (
                t("common.save") || "保存设置"
              )}
            </Button>
          </div>
        </div>
      </div>
    </AppLayout>
  );
}
