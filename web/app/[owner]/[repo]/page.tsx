import { redirect } from "next/navigation";
import { fetchRepoTree } from "@/lib/repository-api";
import { DocNotFound } from "@/components/repo/doc-not-found";
import { DocsPage, DocsBody } from "fumadocs-ui/page";

interface RepoIndexProps {
  params: Promise<{
    owner: string;
    repo: string;
  }>;
}

function encodeSlug(slug: string) {
  return slug
    .split("/")
    .map((segment) => encodeURIComponent(segment))
    .join("/");
}

async function getTreeData(owner: string, repo: string) {
  try {
    return await fetchRepoTree(owner, repo);
  } catch {
    return null;
  }
}

export default async function RepoIndex({ params }: RepoIndexProps) {
  const { owner, repo } = await params;
  
  const tree = await getTreeData(owner, repo);
  
  // API error, layout will handle
  if (!tree) {
    return null;
  }
  
  // Repository does not exist, layout will handle
  if (!tree.exists) {
    return null;
  }

  // Repository is processing, pending, or failed, layout will handle display
  if (tree.statusName !== "Completed") {
    return null;
  }

  // Has default document, redirect
  if (tree.defaultSlug) {
    redirect(`/${owner}/${repo}/${encodeSlug(tree.defaultSlug)}`);
  }

  // No default document but has catalog, show prompt
  if (tree.nodes.length > 0) {
    return (
      <DocsPage toc={[]}>
        <DocsBody>
          <DocNotFound slug="" />
        </DocsBody>
      </DocsPage>
    );
  }

  // Empty repository, layout will handle
  return null;
}
