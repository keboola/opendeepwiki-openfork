"use client";

import { useCallback, useEffect, useState } from "react";
import { useTranslations } from "@/hooks/use-translations";
import { toast } from "sonner";
import {
  getGitHubStatus,
  getGitHubInstallUrl,
  getInstallationRepos,
  batchImportRepos,
  getDepartments,
  GitHubStatus,
  GitHubRepo,
  GitHubInstallation,
  BatchImportResult,
  AdminDepartment,
} from "@/lib/admin-api";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Checkbox } from "@/components/ui/checkbox";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  RefreshCw,
  ExternalLink,
  GitBranch,
  Star,
  GitFork,
  Lock,
  Globe,
  Loader2,
  Building2,
  CheckCircle2,
  XCircle,
  AlertCircle,
  Plus,
  Download,
} from "lucide-react";

export default function GitHubImportPage() {
  const t = useTranslations();

  // Status
  const [status, setStatus] = useState<GitHubStatus | null>(null);
  const [loading, setLoading] = useState(true);

  // Selected installation
  const [selectedInstallation, setSelectedInstallation] = useState<GitHubInstallation | null>(null);

  // Repos
  const [repos, setRepos] = useState<GitHubRepo[]>([]);
  const [repoTotalCount, setRepoTotalCount] = useState(0);
  const [repoPage, setRepoPage] = useState(1);
  const [repoLoading, setRepoLoading] = useState(false);
  const perPage = 30;

  // Selection
  const [selectedRepos, setSelectedRepos] = useState<Set<string>>(new Set());

  // Import
  const [departments, setDepartments] = useState<AdminDepartment[]>([]);
  const [selectedDepartmentId, setSelectedDepartmentId] = useState<string>("");
  const [languageCode, setLanguageCode] = useState("en");
  const [importing, setImporting] = useState(false);
  const [importResult, setImportResult] = useState<BatchImportResult | null>(null);

  const fetchStatus = useCallback(async () => {
    setLoading(true);
    try {
      const result = await getGitHubStatus();
      setStatus(result);
      if (result.installations.length > 0 && !selectedInstallation) {
        setSelectedInstallation(result.installations[0]);
      }
    } catch (error) {
      toast.error(t("admin.githubImport.fetchStatusFailed"));
    } finally {
      setLoading(false);
    }
  }, [t, selectedInstallation]);

  const fetchDepartments = useCallback(async () => {
    try {
      const result = await getDepartments();
      setDepartments(result);
      if (result.length > 0 && !selectedDepartmentId) {
        setSelectedDepartmentId(result[0].id);
      }
    } catch {
      // Departments may not be available
    }
  }, [selectedDepartmentId]);

  const fetchRepos = useCallback(async () => {
    if (!selectedInstallation) return;
    setRepoLoading(true);
    try {
      const result = await getInstallationRepos(
        selectedInstallation.installationId,
        repoPage,
        perPage
      );
      setRepos(result.repositories);
      setRepoTotalCount(result.totalCount);
    } catch (error) {
      toast.error(t("admin.githubImport.fetchReposFailed"));
    } finally {
      setRepoLoading(false);
    }
  }, [selectedInstallation, repoPage, t]);

  useEffect(() => {
    fetchStatus();
    fetchDepartments();
  }, []);

  useEffect(() => {
    if (selectedInstallation) {
      setRepoPage(1);
      setSelectedRepos(new Set());
      setImportResult(null);
    }
  }, [selectedInstallation]);

  useEffect(() => {
    fetchRepos();
  }, [fetchRepos]);

  const handleConnectNew = async () => {
    try {
      const { url } = await getGitHubInstallUrl();
      window.location.href = url;
    } catch (error) {
      toast.error("Failed to get install URL");
    }
  };

  const toggleRepo = (fullName: string) => {
    setSelectedRepos((prev) => {
      const next = new Set(prev);
      if (next.has(fullName)) {
        next.delete(fullName);
      } else {
        next.add(fullName);
      }
      return next;
    });
  };

  const toggleSelectAll = () => {
    const importableRepos = repos.filter((r) => !r.alreadyImported);
    if (selectedRepos.size === importableRepos.length) {
      setSelectedRepos(new Set());
    } else {
      setSelectedRepos(new Set(importableRepos.map((r) => r.fullName)));
    }
  };

  const handleImport = async () => {
    if (!selectedInstallation || selectedRepos.size === 0 || !selectedDepartmentId) return;

    setImporting(true);
    setImportResult(null);
    try {
      const selectedRepoData = repos
        .filter((r) => selectedRepos.has(r.fullName))
        .map((r) => ({
          fullName: r.fullName,
          name: r.name,
          owner: r.owner,
          cloneUrl: r.cloneUrl,
          defaultBranch: r.defaultBranch,
          private: r.private,
          language: r.language,
          stargazersCount: r.stargazersCount,
          forksCount: r.forksCount,
        }));

      const result = await batchImportRepos({
        installationId: selectedInstallation.installationId,
        departmentId: selectedDepartmentId,
        languageCode,
        repos: selectedRepoData,
      });

      setImportResult(result);
      setSelectedRepos(new Set());
      toast.success(
        t("admin.githubImport.importSuccess")
          .replace("{imported}", result.imported.toString())
          .replace("{skipped}", result.skipped.toString())
      );

      // Refresh repo list to update "already imported" flags
      fetchRepos();
    } catch (error) {
      toast.error(
        error instanceof Error ? error.message : t("admin.githubImport.importFailed")
      );
    } finally {
      setImporting(false);
    }
  };

  const totalPages = Math.ceil(repoTotalCount / perPage);
  const importableRepos = repos.filter((r) => !r.alreadyImported);

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <Loader2 className="h-8 w-8 animate-spin text-primary" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">{t("admin.githubImport.title")}</h1>
          <p className="text-muted-foreground mt-1">
            {t("admin.githubImport.description")}
          </p>
        </div>
        <Button variant="outline" size="sm" onClick={fetchStatus}>
          <RefreshCw className="h-4 w-4 mr-2" />
          {t("admin.githubImport.refresh")}
        </Button>
      </div>

      {/* Status Card */}
      <Card>
        <CardHeader>
          <CardTitle className="text-lg">{t("admin.githubImport.connectionStatus")}</CardTitle>
        </CardHeader>
        <CardContent>
          {!status?.configured ? (
            <div className="flex items-center gap-2 text-amber-600">
              <AlertCircle className="h-5 w-5" />
              <span>{t("admin.githubImport.notConfigured")}</span>
            </div>
          ) : (
            <div className="space-y-4">
              <div className="flex items-center gap-2 text-green-600">
                <CheckCircle2 className="h-5 w-5" />
                <span>{t("admin.githubImport.configured")}</span>
              </div>

              {/* Connected Organizations */}
              {status.installations.length > 0 && (
                <div className="space-y-2">
                  <h3 className="text-sm font-medium">
                    {t("admin.githubImport.connectedOrgs")}
                  </h3>
                  <div className="grid gap-2">
                    {status.installations.map((inst) => (
                      <div
                        key={inst.id}
                        className={`flex items-center justify-between p-3 rounded-lg border cursor-pointer transition-colors ${
                          selectedInstallation?.installationId === inst.installationId
                            ? "border-primary bg-primary/5"
                            : "hover:bg-muted"
                        }`}
                        onClick={() => setSelectedInstallation(inst)}
                      >
                        <div className="flex items-center gap-3">
                          {inst.avatarUrl && (
                            <img
                              src={inst.avatarUrl}
                              alt={inst.accountLogin}
                              className="h-8 w-8 rounded-full"
                            />
                          )}
                          <div>
                            <span className="font-medium">{inst.accountLogin}</span>
                            <Badge variant="secondary" className="ml-2 text-xs">
                              {inst.accountType}
                            </Badge>
                          </div>
                        </div>
                        {inst.departmentName && (
                          <Badge variant="outline">
                            <Building2 className="h-3 w-3 mr-1" />
                            {inst.departmentName}
                          </Badge>
                        )}
                      </div>
                    ))}
                  </div>
                </div>
              )}

              <Button variant="outline" size="sm" onClick={handleConnectNew}>
                <Plus className="h-4 w-4 mr-2" />
                {t("admin.githubImport.connectNew")}
              </Button>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Repository Selection */}
      {selectedInstallation && (
        <Card>
          <CardHeader>
            <CardTitle className="text-lg">
              {t("admin.githubImport.importFrom").replace(
                "{org}",
                selectedInstallation.accountLogin
              )}
            </CardTitle>
            <CardDescription>
              {t("admin.githubImport.totalRepos").replace(
                "{count}",
                repoTotalCount.toString()
              )}
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            {/* Import Options */}
            <div className="flex items-center gap-4 flex-wrap">
              <div className="flex items-center gap-2">
                <label className="text-sm font-medium">
                  {t("admin.githubImport.department")}:
                </label>
                <Select
                  value={selectedDepartmentId}
                  onValueChange={setSelectedDepartmentId}
                >
                  <SelectTrigger className="w-[200px]">
                    <SelectValue placeholder={t("admin.githubImport.selectDepartment")} />
                  </SelectTrigger>
                  <SelectContent>
                    {departments.map((dept) => (
                      <SelectItem key={dept.id} value={dept.id}>
                        {dept.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              <div className="flex items-center gap-2">
                <label className="text-sm font-medium">
                  {t("admin.githubImport.language")}:
                </label>
                <Select value={languageCode} onValueChange={setLanguageCode}>
                  <SelectTrigger className="w-[120px]">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="en">English</SelectItem>
                    <SelectItem value="zh">Chinese</SelectItem>
                    <SelectItem value="ko">Korean</SelectItem>
                    <SelectItem value="ja">Japanese</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>

            {/* Select All */}
            <div className="flex items-center gap-2 border-b pb-2">
              <Checkbox
                checked={
                  importableRepos.length > 0 &&
                  selectedRepos.size === importableRepos.length
                }
                onCheckedChange={toggleSelectAll}
              />
              <span className="text-sm font-medium">
                {t("admin.githubImport.selectAll")}
                {selectedRepos.size > 0 &&
                  ` (${selectedRepos.size} ${t("admin.githubImport.selected")})`}
              </span>
            </div>

            {/* Repository List */}
            {repoLoading ? (
              <div className="flex items-center justify-center py-8">
                <Loader2 className="h-6 w-6 animate-spin text-primary" />
              </div>
            ) : (
              <div className="space-y-1">
                {repos.map((repo) => (
                  <div
                    key={repo.fullName}
                    className={`flex items-center gap-3 p-3 rounded-lg border transition-colors ${
                      repo.alreadyImported
                        ? "opacity-50 bg-muted"
                        : selectedRepos.has(repo.fullName)
                        ? "border-primary bg-primary/5"
                        : "hover:bg-muted"
                    }`}
                  >
                    <Checkbox
                      checked={selectedRepos.has(repo.fullName)}
                      disabled={repo.alreadyImported}
                      onCheckedChange={() => toggleRepo(repo.fullName)}
                    />
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2">
                        <GitBranch className="h-4 w-4 text-muted-foreground" />
                        <span className="font-medium truncate">{repo.fullName}</span>
                        {repo.private ? (
                          <Lock className="h-3 w-3 text-amber-500" />
                        ) : (
                          <Globe className="h-3 w-3 text-green-500" />
                        )}
                        {repo.alreadyImported && (
                          <Badge variant="secondary" className="text-xs">
                            {t("admin.githubImport.alreadyImported")}
                          </Badge>
                        )}
                      </div>
                      {repo.description && (
                        <p className="text-xs text-muted-foreground mt-0.5 truncate">
                          {repo.description}
                        </p>
                      )}
                    </div>
                    <div className="flex items-center gap-3 text-xs text-muted-foreground shrink-0">
                      {repo.language && (
                        <span className="px-2 py-0.5 bg-muted rounded text-xs">
                          {repo.language}
                        </span>
                      )}
                      <span className="flex items-center gap-1">
                        <Star className="h-3 w-3" />
                        {repo.stargazersCount}
                      </span>
                      <span className="flex items-center gap-1">
                        <GitFork className="h-3 w-3" />
                        {repo.forksCount}
                      </span>
                      <a
                        href={repo.htmlUrl}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="hover:text-primary"
                        onClick={(e) => e.stopPropagation()}
                      >
                        <ExternalLink className="h-3 w-3" />
                      </a>
                    </div>
                  </div>
                ))}
              </div>
            )}

            {/* Pagination */}
            {totalPages > 1 && (
              <div className="flex items-center justify-between pt-2">
                <Button
                  variant="outline"
                  size="sm"
                  disabled={repoPage <= 1}
                  onClick={() => setRepoPage((p) => p - 1)}
                >
                  {t("admin.githubImport.prevPage")}
                </Button>
                <span className="text-sm text-muted-foreground">
                  {repoPage} / {totalPages}
                </span>
                <Button
                  variant="outline"
                  size="sm"
                  disabled={repoPage >= totalPages}
                  onClick={() => setRepoPage((p) => p + 1)}
                >
                  {t("admin.githubImport.nextPage")}
                </Button>
              </div>
            )}

            {/* Import Button */}
            <div className="flex items-center justify-between pt-4 border-t">
              <span className="text-sm text-muted-foreground">
                {selectedRepos.size > 0
                  ? t("admin.githubImport.readyToImport").replace(
                      "{count}",
                      selectedRepos.size.toString()
                    )
                  : t("admin.githubImport.selectReposPrompt")}
              </span>
              <Button
                onClick={handleImport}
                disabled={
                  selectedRepos.size === 0 || !selectedDepartmentId || importing
                }
              >
                {importing ? (
                  <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                ) : (
                  <Download className="h-4 w-4 mr-2" />
                )}
                {importing
                  ? t("admin.githubImport.importing")
                  : t("admin.githubImport.importButton").replace(
                      "{count}",
                      selectedRepos.size.toString()
                    )}
              </Button>
            </div>

            {/* Import Result */}
            {importResult && (
              <Card className="border-green-200 bg-green-50 dark:border-green-900 dark:bg-green-950">
                <CardContent className="pt-4">
                  <div className="flex items-start gap-3">
                    <CheckCircle2 className="h-5 w-5 text-green-600 mt-0.5" />
                    <div className="space-y-1">
                      <p className="font-medium text-green-800 dark:text-green-200">
                        {t("admin.githubImport.importComplete")}
                      </p>
                      <p className="text-sm text-green-700 dark:text-green-300">
                        {importResult.imported} {t("admin.githubImport.imported")},{" "}
                        {importResult.skipped} {t("admin.githubImport.skipped")}
                      </p>
                      {importResult.skippedRepos.length > 0 && (
                        <p className="text-xs text-green-600 dark:text-green-400">
                          {t("admin.githubImport.skippedList")}:{" "}
                          {importResult.skippedRepos.join(", ")}
                        </p>
                      )}
                    </div>
                  </div>
                </CardContent>
              </Card>
            )}
          </CardContent>
        </Card>
      )}

      {/* Not Configured Guide */}
      {status && !status.configured && (
        <Card>
          <CardHeader>
            <CardTitle className="text-lg">{t("admin.githubImport.setupGuide")}</CardTitle>
          </CardHeader>
          <CardContent>
            <ol className="list-decimal list-inside space-y-2 text-sm text-muted-foreground">
              <li>{t("admin.githubImport.step1")}</li>
              <li>{t("admin.githubImport.step2")}</li>
              <li>{t("admin.githubImport.step3")}</li>
              <li>{t("admin.githubImport.step4")}</li>
            </ol>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
