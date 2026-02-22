"use client"

import * as React from "react"
import { useTranslations } from "next-intl"
import { Send, Loader2, X, ImagePlus, Trash2, RefreshCw, GripVertical, Share2, Copy, Check } from "lucide-react"
import { cn } from "@/lib/utils"
import { Button } from "@/components/ui/button"
import { Textarea } from "@/components/ui/textarea"
import { ScrollArea } from "@/components/ui/scroll-area"
import { useChatHistory } from "@/hooks/use-chat-history"
import { useLocale } from "next-intl"
import {
  streamChat,
  getAvailableModels,
  getChatConfig,
  createChatShare,
  toChatMessageDto,
  toChatShareMessage,
  DocContext,
  ModelConfig,
  ToolCall,
  ToolResult,
  ErrorInfo,
  ChatErrorCodes,
  getErrorMessage,
  isRetryableError,
  ThinkingEvent,
  ToolCallEvent,
  ChatShareResponse,
} from "@/lib/chat-api"
import { ContentBlock } from "@/hooks/use-chat-history"
import { ModelSelector } from "./model-selector"
import { ChatMessageItem } from "./chat-message"
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"

const MIN_WIDTH = 320
const MAX_WIDTH = 800
const DEFAULT_WIDTH = 420
const STORAGE_KEY = "chat-panel-width"

/**
 * Chat panel props
 */
export interface ChatPanelProps {
  /** Whether the panel is expanded */
  isOpen: boolean
  /** Close callback */
  onClose: () => void
  /** Document context */
  context: DocContext
  /** App ID (embed mode) */
  appId?: string
}

/**
 * Error state
 */
interface ErrorState {
  message: string
  code?: string
  retryable: boolean
  retryAfterMs?: number
}

/**
 * Chat panel component
 *
 * Contains message list, input box, send button, model selector.
 * Supports Markdown rendering, tool call display, error handling and retry.
 *
 * Requirements: 2.1, 2.2, 2.3, 2.5, 2.6, 11.1, 11.2, 11.3, 11.4
 */
export function ChatPanel({
  isOpen,
  onClose,
  context,
  appId,
}: ChatPanelProps) {
  const locale = useLocale()
  const t = useTranslations("chat")
  const { messages, addMessage, updateMessage, clearHistory } = useChatHistory()
  const [input, setInput] = React.useState("")
  const [images, setImages] = React.useState<string[]>([])
  const [isLoading, setIsLoading] = React.useState(false)
  const [models, setModels] = React.useState<ModelConfig[]>([])
  const [selectedModelId, setSelectedModelId] = React.useState("")
  const [isEnabled, setIsEnabled] = React.useState(true)
  const [enableImageUpload, setEnableImageUpload] = React.useState(false)
  const [error, setError] = React.useState<ErrorState | null>(null)
  const [isShareDialogOpen, setIsShareDialogOpen] = React.useState(false)
  const [shareTitle, setShareTitle] = React.useState("")
  const [shareDescription, setShareDescription] = React.useState("")
  const [shareExpireMinutes, setShareExpireMinutes] = React.useState(60 * 24 * 7)
  const [shareLoading, setShareLoading] = React.useState(false)
  const [shareResult, setShareResult] = React.useState<ChatShareResponse | null>(null)
  const [shareError, setShareError] = React.useState<string | null>(null)
  const [shareCopied, setShareCopied] = React.useState(false)
  // Quoted selected text (including title)
  const [quotedText, setQuotedText] = React.useState<{ title?: string; text: string } | null>(null)
  const [lastRequest, setLastRequest] = React.useState<{
    input: string
    images: string[]
    userMessageId: string
    assistantMessageId: string
  } | null>(null)
  
  // Panel width state
  const [panelWidth, setPanelWidth] = React.useState(DEFAULT_WIDTH)
  const panelRef = React.useRef<HTMLDivElement>(null)
  const isDraggingRef = React.useRef(false)
  const rafRef = React.useRef<number | null>(null)
  const messagesEndRef = React.useRef<HTMLDivElement>(null)
  const inputRef = React.useRef<HTMLTextAreaElement>(null)
  const fileInputRef = React.useRef<HTMLInputElement>(null)
  const abortControllerRef = React.useRef<AbortController | null>(null)

  const resetShareState = React.useCallback(() => {
    setShareDescription("")
    setShareResult(null)
    setShareError(null)
    setShareCopied(false)
    setShareLoading(false)
  }, [])

  const getSuggestedShareTitle = React.useCallback(() => {
    const lastUserMessage = [...messages].reverse().find(m => m.role === "user" && m.content?.trim())
    if (lastUserMessage?.content) {
      const trimmed = lastUserMessage.content.trim()
      return trimmed.length > 40 ? `${trimmed.slice(0, 40)}…` : trimmed
    }
    return context.currentDocPath || t("assistant.title")
  }, [messages, context.currentDocPath, t])

  const handleOpenShareDialog = React.useCallback(() => {
    if (messages.length === 0) return
    resetShareState()
    setShareTitle(getSuggestedShareTitle())
    setShareExpireMinutes(60 * 24 * 7)
    setIsShareDialogOpen(true)
  }, [messages.length, resetShareState, getSuggestedShareTitle])

  const handleShareDialogChange = React.useCallback((open: boolean) => {
    setIsShareDialogOpen(open)
    if (!open) {
      resetShareState()
    }
  }, [resetShareState])

  const shareLink = React.useMemo(() => {
    if (!shareResult) return ""
    if (typeof window === "undefined") {
      return `https://opendeepwiki.com/share/${shareResult.shareId}`
    }
    return `${window.location.origin}/share/${shareResult.shareId}`
  }, [shareResult])

  const handleCopyShareLink = React.useCallback(async () => {
    if (!shareLink) return
    try {
      await navigator.clipboard.writeText(shareLink)
      setShareCopied(true)
      setTimeout(() => setShareCopied(false), 1800)
    } catch {
      // ignore
    }
  }, [shareLink])

  const handleCreateShare = React.useCallback(async () => {
    if (messages.length === 0) {
      setShareError("No conversation to share")
      return
    }

    const shareModelId = selectedModelId || models.find(m => m.isEnabled)?.id || models[0]?.id
    if (!shareModelId) {
      setShareError("Please select a model first")
      return
    }

    setShareLoading(true)
    setShareError(null)
    try {
      const payload = {
        messages: messages.map(toChatShareMessage),
        context: {
          ...context,
          userLanguage: locale,
        },
        modelId: shareModelId,
        title: shareTitle.trim() || undefined,
        description: shareDescription.trim() || undefined,
        expireMinutes: shareExpireMinutes,
      }
      const result = await createChatShare(payload)
      setShareResult(result)
    } catch (err) {
      setShareError(err instanceof Error ? err.message : "Share failed, please try again")
    } finally {
      setShareLoading(false)
    }
  }, [messages, selectedModelId, models, context, locale, shareTitle, shareDescription, shareExpireMinutes])

  // Load saved width from localStorage
  React.useEffect(() => {
    const saved = localStorage.getItem(STORAGE_KEY)
    if (saved) {
      const width = parseInt(saved, 10)
      if (width >= MIN_WIDTH && width <= MAX_WIDTH) {
        setPanelWidth(width)
      }
    }
  }, [])

  // Drag to resize width
  const handleResizeStart = React.useCallback((e: React.MouseEvent) => {
    e.preventDefault()
    isDraggingRef.current = true
    document.body.style.cursor = "col-resize"
    document.body.style.userSelect = "none"
  }, [])

  React.useEffect(() => {
    const handleMouseMove = (e: MouseEvent) => {
      if (!isDraggingRef.current) return
      
      if (rafRef.current) {
        cancelAnimationFrame(rafRef.current)
      }
      
      rafRef.current = requestAnimationFrame(() => {
        const newWidth = window.innerWidth - e.clientX
        const clampedWidth = Math.min(MAX_WIDTH, Math.max(MIN_WIDTH, newWidth))
        setPanelWidth(clampedWidth)
      })
    }

    const handleMouseUp = () => {
      if (isDraggingRef.current) {
        isDraggingRef.current = false
        document.body.style.cursor = ""
        document.body.style.userSelect = ""
        // Save to localStorage
        localStorage.setItem(STORAGE_KEY, panelWidth.toString())
      }
      if (rafRef.current) {
        cancelAnimationFrame(rafRef.current)
        rafRef.current = null
      }
    }

    document.addEventListener("mousemove", handleMouseMove)
    document.addEventListener("mouseup", handleMouseUp)

    return () => {
      document.removeEventListener("mousemove", handleMouseMove)
      document.removeEventListener("mouseup", handleMouseUp)
      if (rafRef.current) {
        cancelAnimationFrame(rafRef.current)
      }
    }
  }, [panelWidth])

  // Load config and model list
  React.useEffect(() => {
    if (!isOpen) return

    const loadConfig = async () => {
      try {
        const [config, modelList] = await Promise.all([
          getChatConfig(),
          getAvailableModels(),
        ])
        setIsEnabled(config.isEnabled)
        setEnableImageUpload(config.enableImageUpload)
        setModels(modelList)
        
        // Set default model
        if (config.defaultModelId) {
          setSelectedModelId(config.defaultModelId)
        } else if (modelList.length > 0) {
          const enabledModel = modelList.find(m => m.isEnabled)
          if (enabledModel) {
            setSelectedModelId(enabledModel.id)
          }
        }
      } catch (err) {
        console.error(t("error.loadConfigFailed"), err)
        setError({
          message: t("assistant.loadConfigFailed"),
          code: ChatErrorCodes.CONFIG_MISSING,
          retryable: true,
        })
      }
    }

    loadConfig()
  }, [isOpen])

  // Cancel request on component unmount
  React.useEffect(() => {
    return () => {
      if (abortControllerRef.current) {
        abortControllerRef.current.abort()
      }
    }
  }, [])

  // Function to scroll to bottom
  const scrollToBottom = React.useCallback((smooth = true) => {
    if (messagesEndRef.current) {
      messagesEndRef.current.scrollIntoView({ 
        behavior: smooth ? "smooth" : "instant",
        block: "end" 
      })
    }
  }, [])

  // Scroll to bottom when messages change
  React.useEffect(() => {
    // Use requestAnimationFrame to ensure DOM is updated before scrolling
    requestAnimationFrame(() => {
      scrollToBottom()
    })
  }, [messages, scrollToBottom])

  // Keep scrolling to bottom while AI is replying
  React.useEffect(() => {
    if (isLoading) {
      const interval = setInterval(() => scrollToBottom(false), 100)
      return () => clearInterval(interval)
    }
  }, [isLoading, scrollToBottom])

  // Listen for user text selection (only in document content area)
  React.useEffect(() => {
    if (!isOpen) return

    const handleSelectionChange = () => {
      const selection = window.getSelection()
      const text = selection?.toString().trim()
      
      // If no text is selected, clear the quote
      if (!text) {
        setQuotedText(null)
        return
      }
      
      const anchorNode = selection?.anchorNode
      if (!anchorNode) return
      
      // Check if the selected text is in the document content area
      const docContentSelectors = [
        '[data-doc-content]',
        '.prose',
        '.markdown-body',
        'article',
        'main',
      ]
      
      const parentElement = anchorNode.parentElement
      if (!parentElement) return
      
      // Check if within document content area
      const isInDocContent = docContentSelectors.some(selector => 
        parentElement.closest(selector) !== null
      )
      
      // Exclude text selected within the chat panel
      const isInChatPanel = panelRef.current?.contains(anchorNode as Node)
      
      if (isInDocContent && !isInChatPanel) {
        const title = context.currentDocPath || document.title
        setQuotedText({ title, text })
      }
    }

    document.addEventListener("mouseup", handleSelectionChange)
    // Listen for selectionchange event to detect deselection
    document.addEventListener("selectionchange", () => {
      const selection = window.getSelection()
      if (!selection?.toString().trim()) {
        setQuotedText(null)
      }
    })
    
    return () => {
      document.removeEventListener("mouseup", handleSelectionChange)
    }
  }, [isOpen, context.currentDocPath])

  // Handle image upload
  const handleImageUpload = (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files
    if (!files) return

    Array.from(files).forEach(file => {
      // Check file type
      if (!["image/png", "image/jpeg", "image/gif", "image/webp"].includes(file.type)) {
        setError({
          message: t("image.unsupportedFormat"),
          retryable: false,
        })
        return
      }

      // Check file size (10MB)
      if (file.size > 10 * 1024 * 1024) {
        setError({
          message: t("image.sizeTooLarge"),
          retryable: false,
        })
        return
      }

      const reader = new FileReader()
      reader.onload = () => {
        const base64 = reader.result as string
        setImages(prev => [...prev, base64])
      }
      reader.readAsDataURL(file)
    })

    // Clear input to allow re-selecting the same file
    e.target.value = ""
  }

  // Remove image
  const removeImage = (index: number) => {
    setImages(prev => prev.filter((_, i) => i !== index))
  }

  // Send message
  const handleSend = async () => {
    const trimmedInput = input.trim()
    if (!trimmedInput && images.length === 0 && !quotedText) return
    if (!selectedModelId) {
      setError({
        message: t("assistant.selectModel"),
        code: ChatErrorCodes.MODEL_UNAVAILABLE,
        retryable: false,
      })
      return
    }

    setError(null)
    setIsLoading(true)

    // Create new AbortController
    abortControllerRef.current = new AbortController()

    // Add user message (quoted text stored separately, not merged into content)
    const userMessageId = addMessage({
      role: "user",
      content: trimmedInput,
      images: images.length > 0 ? [...images] : undefined,
      quotedText: quotedText || undefined,
    })

    // Clear input
    const savedInput = input
    const savedImages = [...images]
    const savedQuotedText = quotedText
    setInput("")
    setImages([])
    setQuotedText(null)

    // Prepare request
    const allMessages = [...messages, {
      id: userMessageId,
      role: "user" as const,
      content: trimmedInput,
      images: images.length > 0 ? [...images] : undefined,
      quotedText: savedQuotedText || undefined,
      timestamp: Date.now(),
    }]

    // Add AI message placeholder
    const assistantMessageId = addMessage({
      role: "assistant",
      content: "",
    })

    // Save request info for retry
    setLastRequest({
      input: savedInput,
      images: savedImages,
      userMessageId,
      assistantMessageId,
    })

    let assistantContent = ""
    let contentBlocks: ContentBlock[] = []
    let currentToolCalls: ToolCall[] = []
    // Used to track the currently building content block
    let currentThinkingContent = ""

    try {
      const stream = streamChat(
        {
          messages: allMessages.map(toChatMessageDto),
          modelId: selectedModelId,
          context: {
            ...context,
            userLanguage: locale,
          },
          appId,
        },
        {
          signal: abortControllerRef.current.signal,
        }
      )

      for await (const event of stream) {
        switch (event.type) {
          case "content":
            const textContent = event.data as string
            assistantContent += textContent
            // Add or update text content block
            const lastBlock = contentBlocks[contentBlocks.length - 1]
            if (lastBlock && lastBlock.type === "text") {
              lastBlock.content = (lastBlock.content || "") + textContent
            } else {
              contentBlocks.push({ type: "text", content: textContent })
            }
            updateMessage(assistantMessageId, { 
              content: assistantContent, 
              contentBlocks: [...contentBlocks],
              toolCalls: currentToolCalls.length > 0 ? currentToolCalls : undefined
            })
            break

          case "thinking":
            const thinkingEvent = event.data as ThinkingEvent
            if (thinkingEvent.type === "start") {
              // Start a new thinking block
              currentThinkingContent = ""
              contentBlocks.push({ type: "thinking", content: "" })
            } else if (thinkingEvent.type === "delta" && thinkingEvent.content) {
              currentThinkingContent += thinkingEvent.content
              // Update the last thinking block
              const thinkingBlock = contentBlocks.findLast(b => b.type === "thinking")
              if (thinkingBlock) {
                thinkingBlock.content = currentThinkingContent
              }
              updateMessage(assistantMessageId, { 
                content: assistantContent, 
                thinking: currentThinkingContent,
                contentBlocks: [...contentBlocks],
                toolCalls: currentToolCalls.length > 0 ? currentToolCalls : undefined
              })
            }
            break

          case "tool_call":
            const toolCallEvent = event.data as ToolCallEvent
            // Check if a tool call with the same ID already exists
            const existingIndex = currentToolCalls.findIndex(t => t.id === toolCallEvent.id)
            
            if (existingIndex >= 0) {
              // Update existing tool call (add arguments)
              if (toolCallEvent.arguments) {
                currentToolCalls[existingIndex].arguments = toolCallEvent.arguments
                // Update corresponding contentBlock
                const blockIndex = contentBlocks.findIndex(
                  b => b.type === "tool_call" && b.toolCall?.id === toolCallEvent.id
                )
                if (blockIndex >= 0) {
                  contentBlocks[blockIndex].toolCall = currentToolCalls[existingIndex]
                }
              }
            } else {
              // New tool call
              const newToolCall: ToolCall = {
                id: toolCallEvent.id,
                name: toolCallEvent.name,
                arguments: toolCallEvent.arguments || {}
              }
              currentToolCalls = [...currentToolCalls, newToolCall]
              contentBlocks.push({ type: "tool_call", toolCall: newToolCall })
            }
            
            updateMessage(assistantMessageId, {
              content: assistantContent,
              contentBlocks: [...contentBlocks],
              toolCalls: [...currentToolCalls],
            })
            break

          case "tool_result":
            const toolResult = event.data as ToolResult
            // Add tool result message
            addMessage({
              role: "tool",
              content: toolResult.result,
              toolResult,
            })
            break

          case "done":
            // Conversation complete, clear retry info
            setLastRequest(null)
            break

          case "error":
            const errorInfo = event.data as ErrorInfo
            setError({
              message: errorInfo.message || getErrorMessage(errorInfo.code),
              code: errorInfo.code,
              retryable: errorInfo.retryable ?? isRetryableError(errorInfo.code),
              retryAfterMs: errorInfo.retryAfterMs,
            })
            break
        }
      }
    } catch (err) {
      console.error(t("error.chatFailed"), err)
      setError({
        message: err instanceof Error ? err.message : t("error.chatFailed"),
        retryable: true,
      })
    } finally {
      setIsLoading(false)
      abortControllerRef.current = null
    }
  }

  // Retry send
  const handleRetry = async () => {
    if (!lastRequest) return

    // Restore input state
    setInput(lastRequest.input)
    setImages(lastRequest.images)
    setError(null)
    
    // Resend
    handleSend()
  }

  // Handle keyboard events
  const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault()
      handleSend()
    }
  }

  if (!isOpen) return null

  const enabledModels = models.filter(m => m.isEnabled)
  const canSend = (input.trim() || images.length > 0) && selectedModelId && !isLoading

  return (
    <>
      {/* Background overlay - does not close panel, visual separator only */}
      <div
        className="fixed inset-0 z-40 bg-black/20 pointer-events-none"
      />

      {/* Chat panel */}
      <div
        ref={panelRef}
        style={{ width: panelWidth }}
        className={cn(
          "fixed right-0 top-0 z-50 flex h-full flex-col",
          "bg-background shadow-xl",
          "transform transition-transform duration-300 ease-in-out",
          isOpen ? "translate-x-0" : "translate-x-full"
        )}
      >
        {/* Left drag handle */}
        <div
          onMouseDown={handleResizeStart}
          className="absolute left-0 top-0 bottom-0 w-1 cursor-col-resize hover:bg-primary/20 active:bg-primary/30 transition-colors group flex items-center"
        >
          <div className="absolute left-0 w-4 h-full" />
          <GripVertical className="h-6 w-6 text-muted-foreground/30 group-hover:text-muted-foreground/60 -ml-2.5" />
        </div>

        {/* Header */}
        <div className="flex items-center justify-between px-4 py-3">
          <h2 className="font-semibold">{t("assistant.title")}</h2>
          <div className="flex items-center gap-1">
            <Button
              variant="ghost"
              size="icon"
              onClick={handleOpenShareDialog}
              title="Share conversation"
              disabled={messages.length === 0}
            >
              <Share2 className="h-4 w-4" />
            </Button>
            <Button
              variant="ghost"
              size="icon"
              onClick={clearHistory}
              title={t("panel.clearHistory")}
              disabled={messages.length === 0}
            >
              <Trash2 className="h-4 w-4" />
            </Button>
            <Button variant="ghost" size="icon" onClick={onClose}>
              <X className="h-4 w-4" />
            </Button>
          </div>
        </div>

        {/* Message list - leave space at bottom for floating input box */}
        <ScrollArea className="flex-1 overflow-hidden w-full">
          <div className="flex flex-col w-full">
            {!isEnabled ? (
              <div className="flex h-full items-center justify-center p-8 text-center text-muted-foreground">
                {t("assistant.disabled")}
              </div>
            ) : enabledModels.length === 0 ? (
              <div className="flex h-full items-center justify-center p-8 text-center text-muted-foreground">
                {t("assistant.noModels")}
              </div>
            ) : messages.length === 0 ? (
              <div className="flex h-full items-center justify-center p-8 text-center text-muted-foreground">
                <div>
                  <p className="mb-2">{t("assistant.greeting")}</p>
                  <p className="text-sm">{t("assistant.greetingSubtitle")}</p>
                </div>
              </div>
            ) : (
              <div className="w-full">
                {messages.map((message, index) => (
                  <div
                    key={message.id}
                    className="animate-in fade-in-0 slide-in-from-bottom-2 duration-300"
                    style={{ animationDelay: `${Math.min(index * 50, 200)}ms` }}
                  >
                    <ChatMessageItem message={message} />
                  </div>
                ))}
              </div>
            )}

            {/* Loading indicator */}
            {isLoading && (
              <div className="flex items-center gap-2 p-4 text-muted-foreground animate-in fade-in-0 duration-200">
                <div className="flex gap-1">
                  <span className="w-2 h-2 bg-primary/60 rounded-full animate-bounce" style={{ animationDelay: '0ms' }} />
                  <span className="w-2 h-2 bg-primary/60 rounded-full animate-bounce" style={{ animationDelay: '150ms' }} />
                  <span className="w-2 h-2 bg-primary/60 rounded-full animate-bounce" style={{ animationDelay: '300ms' }} />
                </div>
                <span className="text-sm">{t("assistant.thinking")}</span>
              </div>
            )}
            
            {/* Scroll anchor + bottom whitespace to prevent content from being hidden by input box */}
            <div ref={messagesEndRef} className="h-52 shrink-0" />
          </div>
        </ScrollArea>

        {/* Error message */}
        {error && (
          <div className="absolute bottom-44 left-4 right-4 rounded-lg border border-destructive/50 bg-destructive/10 px-4 py-2 text-sm text-destructive shadow-lg">
            <div className="flex items-center justify-between">
              <span>{error.message}</span>
              <div className="flex items-center gap-2">
                {error.retryable && lastRequest && (
                  <button
                    className="flex items-center gap-1 underline hover:no-underline"
                    onClick={handleRetry}
                    disabled={isLoading}
                  >
                    <RefreshCw className="h-3 w-3" />
                    Retry
                  </button>
                )}
                <button
                  className="underline hover:no-underline"
                  onClick={() => setError(null)}
                >
                  Close
                </button>
              </div>
            </div>
          </div>
        )}

        {/* Floating input area - ChatGPT style */}
        <div className="absolute bottom-0 left-0 right-0 p-3">
          <div className="rounded-2xl border border-border/50 bg-background/90 backdrop-blur-sm shadow-lg">
            {/* Quoted text preview */}
            {quotedText && (
              <div className="border-b border-border/50 px-3 py-2">
                <div className="flex items-start justify-between gap-2">
                  <div className="flex-1 overflow-hidden">
                    <div className="text-xs text-primary font-medium mb-1 flex items-center gap-1">
                      <span>{t("quote.icon")}</span>
                      <span>{t("quote.label")}{quotedText.title || t("message.currentPage")}</span>
                    </div>
                    <pre className="text-xs text-muted-foreground whitespace-pre-wrap break-words max-h-16 overflow-y-auto">
                      {quotedText.text}
                    </pre>
                  </div>
                  <button
                    type="button"
                    onClick={() => setQuotedText(null)}
                    className="shrink-0 rounded p-1 hover:bg-muted"
                  >
                    <X className="h-3 w-3 text-muted-foreground" />
                  </button>
                </div>
              </div>
            )}

            {/* Image preview */}
            {images.length > 0 && (
              <div className="border-b border-border/50 px-3 py-2">
                <div className="flex flex-wrap gap-2">
                  {images.map((img, index) => (
                    <div key={index} className="relative">
                      <img
                        src={img}
                        alt={t("image.preview", { index: index + 1 })}
                        className="h-10 w-10 rounded-lg object-cover border"
                      />
                      <button
                        type="button"
                        onClick={() => removeImage(index)}
                        className="absolute -right-1 -top-1 rounded-full bg-destructive p-0.5 text-destructive-foreground shadow"
                      >
                        <X className="h-3 w-3" />
                      </button>
                    </div>
                  ))}
                </div>
              </div>
            )}

            {/* Input box */}
            <div className="px-3 py-1.5">
              <Textarea
                ref={inputRef}
                value={input}
                onChange={(e) => setInput(e.target.value)}
                onKeyDown={handleKeyDown}
                placeholder={t("panel.inputPlaceholder")}
                className="min-h-[100px] resize-none border-0 !bg-transparent p-0 text-sm leading-5 focus-visible:ring-0 focus-visible:ring-offset-0 placeholder:text-muted-foreground/60 shadow-none"
                disabled={!isEnabled || enabledModels.length === 0 || isLoading}
                rows={5}
              />
            </div>

            {/* Bottom toolbar */}
            <div className="flex items-center justify-between border-t border-border/50 px-2 py-1">
              {/* Left buttons */}
              <div className="flex items-center gap-1">
                <input
                  ref={fileInputRef}
                  type="file"
                  accept="image/png,image/jpeg,image/gif,image/webp"
                  multiple
                  className="hidden"
                  onChange={handleImageUpload}
                />
                {enableImageUpload && (
                  <Button
                    variant="ghost"
                    size="icon"
                    className="h-6 w-6 rounded-md"
                    onClick={() => fileInputRef.current?.click()}
                    disabled={!isEnabled || enabledModels.length === 0}
                    title={t("panel.uploadImage")}
                  >
                    <ImagePlus className="h-3.5 w-3.5" />
                  </Button>
                )}
              </div>

              {/* Right side: model selector + send button */}
              <div className="flex items-center gap-1.5">
                <ModelSelector
                  models={models}
                  selectedModelId={selectedModelId}
                  onModelChange={setSelectedModelId}
                  disabled={isLoading}
                />
                <Button
                  onClick={handleSend}
                  disabled={!canSend}
                  size="icon"
                  className={cn(
                    "h-6 w-6 rounded-md transition-all",
                    canSend ? "bg-primary hover:bg-primary/90" : "bg-muted text-muted-foreground"
                  )}
                >
                  {isLoading ? (
                    <Loader2 className="h-3.5 w-3.5 animate-spin" />
                  ) : (
                    <Send className="h-3.5 w-3.5" />
                  )}
                </Button>
              </div>
            </div>
          </div>
        </div>
      </div>
      <Dialog open={isShareDialogOpen} onOpenChange={handleShareDialogChange}>
        <DialogContent className="sm:max-w-[520px]">
          <DialogHeader>
            <DialogTitle>Share Conversation</DialogTitle>
            <DialogDescription>
              Generate a read-only link to publicly display an instant snapshot of the current conversation.
            </DialogDescription>
          </DialogHeader>

          {shareResult ? (
            <div className="space-y-4">
              <div className="rounded-xl border border-border bg-muted/40 px-4 py-3 text-sm">
                <div className="text-muted-foreground">Share link</div>
                <p className="mt-1 break-all text-foreground">{shareLink}</p>
                <p className="mt-2 text-xs text-muted-foreground">
                  Share ID: {shareResult.shareId} · Model: {shareResult.modelId}
                </p>
              </div>
              <div className="flex flex-wrap gap-2">
                <Button onClick={handleCopyShareLink} className="gap-2">
                  {shareCopied ? (
                    <>
                      <Check className="h-4 w-4" /> Copied
                    </>
                  ) : (
                    <>
                      <Copy className="h-4 w-4" /> Copy link
                    </>
                  )}
                </Button>
                <Button
                  variant="secondary"
                  onClick={() => window.open(shareLink, "_blank", "noopener,noreferrer")}
                  disabled={!shareLink}
                >
                  Open share page
                </Button>
                <Button variant="ghost" onClick={() => handleShareDialogChange(false)}>
                  Close
                </Button>
              </div>
            </div>
          ) : (
            <>
              <div className="space-y-4">
                <div className="space-y-2">
                  <label className="text-sm font-medium">Share title</label>
                  <Input
                    value={shareTitle}
                    placeholder="Enter a title for the share"
                    onChange={(e) => setShareTitle(e.target.value)}
                  />
                </div>
                <div className="space-y-2">
                  <label className="text-sm font-medium">Description (optional)</label>
                  <Textarea
                    value={shareDescription}
                    rows={3}
                    placeholder="Provide context for viewers"
                    onChange={(e) => setShareDescription(e.target.value)}
                  />
                </div>
                <div className="space-y-2">
                  <label className="text-sm font-medium">Expiration (minutes)</label>
                  <Input
                    type="number"
                    min={10}
                    max={60 * 24 * 30}
                    value={shareExpireMinutes}
                    onChange={(e) => {
                      const value = parseInt(e.target.value, 10)
                      if (!Number.isNaN(value)) {
                        setShareExpireMinutes(Math.min(Math.max(value, 10), 60 * 24 * 30))
                      }
                    }}
                  />
                  <p className="text-xs text-muted-foreground">Default 7 days, maximum 30 days.</p>
                </div>
                {shareError && (
                  <p className="text-sm text-destructive">{shareError}</p>
                )}
              </div>
              <DialogFooter>
                <Button variant="outline" onClick={() => handleShareDialogChange(false)}>
                  Cancel
                </Button>
                <Button onClick={handleCreateShare} disabled={shareLoading}>
                  {shareLoading ? (
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  ) : null}
                  {shareLoading ? "Generating..." : "Generate share link"}
                </Button>
              </DialogFooter>
            </>
          )}
        </DialogContent>
      </Dialog>
    </>
  )
}
