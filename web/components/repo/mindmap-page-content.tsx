"use client";

import { Network, Loader2, AlertCircle } from "lucide-react";
import { DocsPage, DocsBody } from "fumadocs-ui/page";
import { MindMapViewer } from "@/components/repo/mind-map-viewer";
import { useTranslations } from "@/hooks/use-translations";

interface MindMapData {
  content?: string | null;
  statusName?: string;
  branch?: string;
}

interface MindMapPageContentProps {
  owner: string;
  repo: string;
  mindMap: MindMapData | null;
}

export function MindMapPageContent({ owner, repo, mindMap }: MindMapPageContentProps) {
  const t = useTranslations();

  // Mind map does not exist
  if (!mindMap) {
    return (
      <DocsPage toc={[]}>
        <DocsBody>
          <div className="flex flex-col items-center justify-center py-20 text-center">
            <AlertCircle className="h-12 w-12 text-fd-muted-foreground mb-4" />
            <h2 className="text-xl font-semibold mb-2">{t("mindmap.error")}</h2>
            <p className="text-fd-muted-foreground">
              {t("mindmap.errorDescription")}
            </p>
          </div>
        </DocsBody>
      </DocsPage>
    );
  }

  // Mind map is being generated
  if (mindMap.statusName === "Pending" || mindMap.statusName === "Processing") {
    return (
      <DocsPage toc={[]}>
        <DocsBody>
          <div className="flex flex-col items-center justify-center py-20 text-center">
            <Loader2 className="h-12 w-12 text-blue-500 animate-spin mb-4" />
            <h2 className="text-xl font-semibold mb-2">{t("mindmap.loading")}</h2>
            <p className="text-fd-muted-foreground">
              {t("mindmap.loadingDescription")}
            </p>
          </div>
        </DocsBody>
      </DocsPage>
    );
  }

  // Mind map generation failed
  if (mindMap.statusName === "Failed") {
    return (
      <DocsPage toc={[]}>
        <DocsBody>
          <div className="flex flex-col items-center justify-center py-20 text-center">
            <AlertCircle className="h-12 w-12 text-red-500 mb-4" />
            <h2 className="text-xl font-semibold mb-2">{t("mindmap.failed")}</h2>
            <p className="text-fd-muted-foreground">
              {t("mindmap.failedDescription")}
            </p>
          </div>
        </DocsBody>
      </DocsPage>
    );
  }

  // Mind map content is empty
  if (!mindMap.content) {
    return (
      <DocsPage toc={[]}>
        <DocsBody>
          <div className="flex flex-col items-center justify-center py-20 text-center">
            <Network className="h-12 w-12 text-fd-muted-foreground mb-4" />
            <h2 className="text-xl font-semibold mb-2">{t("mindmap.empty")}</h2>
            <p className="text-fd-muted-foreground">
              {t("mindmap.emptyDescription")}
            </p>
          </div>
        </DocsBody>
      </DocsPage>
    );
  }

  return (
    <DocsPage toc={[]}>
      <DocsBody>
        <div className="mb-6">
          <div className="flex items-center gap-3 mb-3">
            <Network className="h-7 w-7 text-blue-500" />
            <h1 className="text-2xl font-bold">{t("mindmap.title")}</h1>
          </div>
          <p className="text-fd-muted-foreground text-sm">
            {t("mindmap.description", { owner, repo })}
          </p>
        </div>
        
        <div className="-mx-4 md:-mx-6 lg:-mx-8">
          <MindMapViewer
            content={mindMap.content}
            owner={owner}
            repo={repo}
            branch={mindMap.branch}
          />
        </div>
      </DocsBody>
    </DocsPage>
  );
}
