"use client";

import * as React from "react";
import { useTranslations } from "next-intl";
import { useRouter, usePathname, useSearchParams } from "next/navigation";
import { GitBranch, Languages } from "lucide-react";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import type { RepoBranchesResponse } from "@/types/repository";

interface BranchLanguageSelectorProps {
  owner: string;
  repo: string;
  branches: RepoBranchesResponse;
  currentBranch: string;
  currentLanguage: string;
}

const languageNames: Record<string, string> = {
  zh: "Simplified Chinese",
  en: "English",
  ko: "Korean",
  ja: "Japanese",
  es: "Spanish",
  fr: "French",
  de: "German",
  pt: "Portuguese",
  ru: "Russian",
  ar: "Arabic",
};

export function BranchLanguageSelector({
  owner,
  repo,
  branches,
  currentBranch,
  currentLanguage,
}: BranchLanguageSelectorProps) {
  const t = useTranslations("common");
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();

  // Get languages supported by the current branch
  const currentBranchData = branches.branches.find(
    (b) => b.name === currentBranch
  );
  const availableLanguages = currentBranchData?.languages ?? branches.languages;

  const handleBranchChange = (newBranch: string) => {
    const params = new URLSearchParams(searchParams.toString());
    params.set("branch", newBranch);
    
    // Check if the new branch supports the current language
    const newBranchData = branches.branches.find((b) => b.name === newBranch);
    if (newBranchData && !newBranchData.languages.includes(currentLanguage)) {
      // If not supported, switch to the first language of this branch
      params.set("lang", newBranchData.languages[0] ?? branches.defaultLanguage);
    }
    
    router.push(`${pathname}?${params.toString()}`);
  };

  const handleLanguageChange = (newLanguage: string) => {
    const params = new URLSearchParams(searchParams.toString());
    params.set("lang", newLanguage);
    if (currentBranch) {
      params.set("branch", currentBranch);
    }
    // Use window.location to force page refresh, ensuring middleware re-executes to update i18n locale
    window.location.href = `${pathname}?${params.toString()}`;
  };

  // If no branch or language data, don't show the selector
  if (branches.branches.length === 0 && branches.languages.length === 0) {
    return null;
  }

  return (
    <div className="flex flex-col gap-2 px-4 py-3 border-b border-border">
      {branches.branches.length > 0 && (
        <div className="flex items-center gap-2">
          <GitBranch className="h-4 w-4 text-muted-foreground shrink-0" />
          <Select value={currentBranch} onValueChange={handleBranchChange}>
            <SelectTrigger className="h-8 text-xs flex-1">
              <SelectValue placeholder={t("branch.selectBranch")} />
            </SelectTrigger>
            <SelectContent>
              {branches.branches.map((branch) => (
                <SelectItem key={branch.name} value={branch.name}>
                  {branch.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      )}
      
      {availableLanguages.length > 0 && (
        <div className="flex items-center gap-2">
          <Languages className="h-4 w-4 text-muted-foreground shrink-0" />
          <Select value={currentLanguage} onValueChange={handleLanguageChange}>
            <SelectTrigger className="h-8 text-xs flex-1">
              <SelectValue placeholder={t("language.selectLanguage")} />
            </SelectTrigger>
            <SelectContent>
              {availableLanguages.map((lang) => (
                <SelectItem key={lang} value={lang}>
                  {languageNames[lang] ?? lang}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      )}
    </div>
  );
}
