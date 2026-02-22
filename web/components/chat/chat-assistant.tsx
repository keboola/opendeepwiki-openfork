"use client"

import * as React from "react"
import { useTranslations } from "next-intl"
import { FloatingBall } from "./floating-ball"
import { ChatPanel } from "./chat-panel"
import { getChatConfig, DocContext, CatalogItem } from "@/lib/chat-api"

/**
 * Chat assistant props
 */
export interface ChatAssistantProps {
  /** Document context */
  context: DocContext
  /** App ID (embedded mode) */
  appId?: string
  /** Custom icon URL */
  iconUrl?: string
}

/**
 * Chat assistant component
 *
 * Integrates floating ball and chat panel, manages expand/collapse state
 *
 * Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 5.1, 5.2
 */
export function ChatAssistant({
  context,
  appId,
  iconUrl,
}: ChatAssistantProps) {
  const t = useTranslations("chat")
  const [isOpen, setIsOpen] = React.useState(false)
  const [isEnabled, setIsEnabled] = React.useState(false)
  const [isLoading, setIsLoading] = React.useState(true)

  // Load config to check if enabled
  React.useEffect(() => {
    const checkEnabled = async () => {
      try {
        const config = await getChatConfig()
        setIsEnabled(config.isEnabled)
      } catch (err) {
        console.error(t("assistant.loadConfigFailed"), err)
        setIsEnabled(false)
      } finally {
        setIsLoading(false)
      }
    }

    checkEnabled()
  }, [])

  const handleToggle = React.useCallback(() => {
    setIsOpen(prev => !prev)
  }, [])

  const handleClose = React.useCallback(() => {
    setIsOpen(false)
  }, [])

  // Don't show while loading
  if (isLoading) {
    return null
  }

  return (
    <>
      <FloatingBall
        enabled={isEnabled}
        iconUrl={iconUrl}
        isOpen={isOpen}
        onToggle={handleToggle}
      />
      <ChatPanel
        isOpen={isOpen}
        onClose={handleClose}
        context={context}
        appId={appId}
      />
    </>
  )
}

/**
 * Build CatalogItem from RepoTreeNode
 */
export interface RepoTreeNode {
  title: string
  slug: string
  children?: RepoTreeNode[]
}

export function buildCatalogMenu(nodes: RepoTreeNode[]): CatalogItem[] {
  return nodes.map(node => ({
    title: node.title,
    path: node.slug,
    children: node.children ? buildCatalogMenu(node.children) : undefined,
  }))
}
