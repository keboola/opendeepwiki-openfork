"use client";

import { FileQuestion } from "lucide-react";

interface DocNotFoundProps {
  slug: string;
}

export function DocNotFound({ slug }: DocNotFoundProps) {
  return (
    <div className="flex flex-col items-center justify-center py-20 px-4">
      <div className="rounded-full bg-muted/50 p-4 mb-6">
        <FileQuestion className="h-12 w-12 text-muted-foreground" />
      </div>
      <h2 className="text-xl font-semibold mb-2">Document not found</h2>
      <p className="text-muted-foreground text-center max-w-md">
        No document found at path <code className="px-1.5 py-0.5 bg-muted rounded text-sm">{slug}</code>.
      </p>
      <p className="text-sm text-muted-foreground mt-4">
        Please select another document from the sidebar.
      </p>
    </div>
  );
}
