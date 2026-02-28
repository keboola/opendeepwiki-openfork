import React from "react";
import { fetchRepoTree, fetchRepoBranches, checkGitHubRepo } from "@/lib/repository-api";
import { getServerAuthHeaders } from "@/lib/server-auth";
import { RepoShell } from "@/components/repo/repo-shell";
import { RepositoryProcessingStatus } from "@/components/repo/repository-processing-status";
import { RepositoryNotFound } from "@/components/repo/repository-not-found";
import { RootProvider } from "fumadocs-ui/provider/next";

// Disable caching
export const dynamic = "force-dynamic";

interface RepoLayoutProps {
  children: React.ReactNode;
  params: Promise<{
    owner: string;
    repo: string;
  }>;
}

async function getTreeData(owner: string, repo: string, headers?: HeadersInit) {
  try {
    const tree = await fetchRepoTree(owner, repo, undefined, undefined, headers);
    return tree;
  } catch {
    return null;
  }
}

async function getBranchesData(owner: string, repo: string, headers?: HeadersInit) {
  try {
    const branches = await fetchRepoBranches(owner, repo, headers);
    return branches;
  } catch {
    return null;
  }
}

async function getGitHubInfo(owner: string, repo: string, headers?: HeadersInit) {
  try {
    return await checkGitHubRepo(owner, repo, headers);
  } catch {
    return null;
  }
}

export default async function RepoLayout({ children, params }: RepoLayoutProps) {
  const { owner, repo } = await params;
  const headers = await getServerAuthHeaders();

  const tree = await getTreeData(owner, repo, headers);

  // API request failed or repository does not exist, check GitHub
  if (!tree || !tree.exists) {
    const gitHubInfo = await getGitHubInfo(owner, repo, headers);
    return <RepositoryNotFound owner={owner} repo={repo} gitHubInfo={gitHubInfo} />;
  }

  // Repository is processing or pending
  if (tree.statusName === "Pending" || tree.statusName === "Processing" || tree.statusName === "Failed") {
    return (
      <RepositoryProcessingStatus
        owner={owner}
        repo={repo}
        status={tree.statusName}
      />
    );
  }

  // Repository completed but has no documents
  if (tree.nodes.length === 0) {
    return (
      <RepositoryProcessingStatus
        owner={owner}
        repo={repo}
        status="Completed"
      />
    );
  }

  // Fetch branch and language data
  const branches = await getBranchesData(owner, repo, headers);

  return (
    <RootProvider>
      <RepoShell 
        owner={owner} 
        repo={repo} 
        initialNodes={tree.nodes}
        initialBranches={branches ?? undefined}
        initialBranch={tree.currentBranch}
        initialLanguage={tree.currentLanguage}
      >
        {children}
      </RepoShell>
    </RootProvider>
  );
}
