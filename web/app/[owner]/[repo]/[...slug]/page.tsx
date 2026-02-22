import { fetchRepoDoc } from "@/lib/repository-api";
import { extractHeadings } from "@/lib/markdown";
import { MarkdownRenderer } from "@/components/repo/markdown-renderer";
import { DocNotFound } from "@/components/repo/doc-not-found";
import { SourceFiles } from "@/components/repo/source-files";
import { DocsPage, DocsBody } from "fumadocs-ui/page";
import type { TOCItemType } from "fumadocs-core/toc";

interface RepoDocPageProps {
  params: Promise<{
    owner: string;
    repo: string;
    slug: string[];
  }>;
  searchParams: Promise<{
    branch?: string;
    lang?: string;
  }>;
}

async function getDocData(owner: string, repo: string, slug: string, branch?: string, lang?: string) {
  try {
    const doc = await fetchRepoDoc(owner, repo, slug, branch, lang);
    if (!doc.exists) {
      return null;
    }
    const headings = extractHeadings(doc.content, 3);
    return { doc, headings };
  } catch {
    return null;
  }
}

export default async function RepoDocPage({ params, searchParams }: RepoDocPageProps) {
  const { owner, repo, slug: slugParts } = await params;
  const resolvedSearchParams = await searchParams;
  const branch = resolvedSearchParams?.branch;
  const lang = resolvedSearchParams?.lang;
  const slug = slugParts.join("/");

  const data = await getDocData(owner, repo, slug, branch, lang);
  
  // Document does not exist, but keep sidebar (provided by layout)
  if (!data) {
    return (
      <DocsPage toc={[]}>
        <DocsBody>
          <DocNotFound slug={slug} />
        </DocsBody>
      </DocsPage>
    );
  }

  const { doc, headings } = data;

  // Convert headings to fumadocs TOC format
  const toc: TOCItemType[] = headings.map((h) => ({
    title: h.text,
    url: `#${h.id}`,
    depth: h.level,
  }));

  return (
    <DocsPage toc={toc}>
      <DocsBody>
        <MarkdownRenderer content={doc.content} />
        <SourceFiles 
          files={doc.sourceFiles || []} 
          branch={branch}
        />
      </DocsBody>
    </DocsPage>
  );
}
