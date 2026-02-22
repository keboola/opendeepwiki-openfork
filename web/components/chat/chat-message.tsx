"use client"

import * as React from "react"
import { useTranslations } from "next-intl"
import { User, Bot, Wrench, ChevronDown, ChevronRight, Coins, Brain, Quote } from "lucide-react"
import { cn } from "@/lib/utils"
import { ChatMessage as ChatMessageType, ToolCall, ToolResult, ContentBlock, QuotedText } from "@/hooks/use-chat-history"
import ReactMarkdown from "react-markdown"
import remarkGfm from "remark-gfm"

/**
 * Message component props
 */
export interface ChatMessageProps {
  message: ChatMessageType
}

/**
 * Quoted text display component
 */
function QuotedTextDisplay({ quotedText }: { quotedText: QuotedText }) {
  const t = useTranslations("chat")
  
  if (!quotedText || !quotedText.text) return null

  return (
    <div className="mb-2 rounded-lg border border-blue-500/30 bg-blue-500/5 overflow-hidden w-full">
      <div className="px-3 py-2">
        <div className="flex items-center gap-2 text-xs text-blue-600 dark:text-blue-400 mb-1">
          <Quote className="h-3 w-3" />
          <span className="font-medium">{t("message.quotedFrom")}{quotedText.title || t("message.currentPage")}</span>
        </div>
        <pre className="text-xs text-muted-foreground whitespace-pre-wrap break-words max-h-24 overflow-y-auto">
          {quotedText.text}
        </pre>
      </div>
    </div>
  )
}

/**
 * Thinking content display component
 */
function ThinkingDisplay({ thinking }: { thinking: string }) {
  const t = useTranslations("chat")
  const [isExpanded, setIsExpanded] = React.useState(false)

  if (!thinking) return null

  return (
    <div className="mb-2 rounded-lg border border-purple-500/30 bg-purple-500/5 overflow-hidden w-full">
      <button
        type="button"
        onClick={() => setIsExpanded(!isExpanded)}
        className="flex w-full items-center gap-2 px-3 py-2 text-left text-sm text-purple-600 dark:text-purple-400 hover:bg-purple-500/10 transition-colors"
      >
        {isExpanded ? (
          <ChevronDown className="h-4 w-4" />
        ) : (
          <ChevronRight className="h-4 w-4" />
        )}
        <Brain className="h-4 w-4" />
        <span className="font-medium">{t("message.thinking")}</span>
      </button>
      {isExpanded && (
        <div className="px-3 pb-3 pt-1">
          <div className="prose prose-sm dark:prose-invert max-w-full prose-p:my-1 text-muted-foreground">
            <ReactMarkdown remarkPlugins={[remarkGfm]}>
              {thinking}
            </ReactMarkdown>
          </div>
        </div>
      )}
    </div>
  )
}

/**
 * Tool call display component
 */
function ToolCallDisplay({ toolCall }: { toolCall: ToolCall }) {
  const t = useTranslations("chat")
  const [isExpanded, setIsExpanded] = React.useState(false)

  return (
    <div className="my-2 rounded-md border border-border bg-muted/50 p-2 text-sm w-full">
      <button
        type="button"
        onClick={() => setIsExpanded(!isExpanded)}
        className="flex w-full items-center gap-1 text-left text-muted-foreground hover:text-foreground"
      >
        {isExpanded ? (
          <ChevronDown className="h-4 w-4" />
        ) : (
          <ChevronRight className="h-4 w-4" />
        )}
        <Wrench className="h-4 w-4" />
        <span className="font-medium">{toolCall.name}</span>
      </button>
      {isExpanded && toolCall.arguments && Object.keys(toolCall.arguments).length > 0 && (
        <pre className="mt-2 overflow-auto rounded bg-background p-2 text-xs">
          {JSON.stringify(toolCall.arguments, null, 2)}
        </pre>
      )}
    </div>
  )
}

/**
 * Tool result display component
 */
function ToolResultDisplay({ toolResult }: { toolResult: ToolResult }) {
  const t = useTranslations("chat")
  const [isExpanded, setIsExpanded] = React.useState(false)

  return (
    <div
      className={cn(
        "my-2 rounded-md border p-2 text-sm w-full",
        toolResult.isError
          ? "border-destructive/50 bg-destructive/10"
          : "border-border bg-muted/50"
      )}
    >
      <button
        type="button"
        onClick={() => setIsExpanded(!isExpanded)}
        className="flex w-full items-center gap-1 text-left text-muted-foreground hover:text-foreground"
      >
        {isExpanded ? (
          <ChevronDown className="h-4 w-4" />
        ) : (
          <ChevronRight className="h-4 w-4" />
        )}
        <span className="font-medium">
          {toolResult.isError ? t("message.toolError") : t("message.toolResult")}
        </span>
      </button>
      {isExpanded && (
        <pre className="mt-2 max-h-40 overflow-auto rounded bg-background p-2 text-xs whitespace-pre-wrap">
          {toolResult.result}
        </pre>
      )}
    </div>
  )
}

/**
 * Text content display component
 */
function TextContentDisplay({ content }: { content: string }) {
  return (
    <div className="rounded-lg px-3 py-2 w-full overflow-hidden bg-muted/50 text-foreground">
      <div className="prose prose-sm dark:prose-invert max-w-none prose-p:my-1 prose-pre:my-2 prose-pre:p-0 prose-pre:bg-transparent break-words">
        <ReactMarkdown 
          remarkPlugins={[remarkGfm]}
          components={{
            pre: ({ children }) => (
              <pre className="overflow-x-auto rounded-lg p-4 text-sm my-2 max-w-full" style={{ backgroundColor: '#18181b' }}>
                {children}
              </pre>
            ),
            code: ({ className, children, ...props }) => {
              const match = /language-(\w+)/.exec(className || '')
              
              if (!className && !match) {
                // Inline code
                return (
                  <code className="rounded px-1.5 py-0.5 text-xs text-zinc-200 font-mono break-all" style={{ backgroundColor: '#27272a' }} {...props}>
                    {children}
                  </code>
                )
              }
              // Code inside code block
              return (
                <code className="text-sm text-zinc-100 font-mono block" {...props}>
                  {children}
                </code>
              )
            },
          }}
        >
          {content}
        </ReactMarkdown>
      </div>
    </div>
  )
}

/**
 * Content block renderer component
 */
function ContentBlockRenderer({ block, index }: { block: ContentBlock; index: number }) {
  switch (block.type) {
    case "thinking":
      return <ThinkingDisplay key={`thinking-${index}`} thinking={block.content || ""} />
    case "tool_call":
      return block.toolCall ? <ToolCallDisplay key={`tool-${index}`} toolCall={block.toolCall} /> : null
    case "text":
      return block.content ? <TextContentDisplay key={`text-${index}`} content={block.content} /> : null
    default:
      return null
  }
}

/**
 * Chat message component
 *
 * Supports displaying user messages, AI replies, and tool call information
 * Renders in contentBlocks order, maintaining call sequence
 *
 * Requirements: 2.3, 2.4, 2.5, 2.6
 */
export function ChatMessageItem({ message }: ChatMessageProps) {
  const t = useTranslations("chat")
  const isUser = message.role === "user"
  const isTool = message.role === "tool"
  
  // Determine whether to render using contentBlocks
  const hasContentBlocks = message.contentBlocks && message.contentBlocks.length > 0

  return (
    <div
      className={cn(
        "flex gap-3 p-3 w-full",
        isUser ? "flex-row-reverse" : "flex-row"
      )}
    >
      {/* Avatar */}
      <div
        className={cn(
          "flex h-8 w-8 shrink-0 items-center justify-center rounded-full",
          isUser
            ? "bg-primary text-primary-foreground"
            : isTool
            ? "bg-amber-500 text-white"
            : "bg-muted text-muted-foreground"
        )}
      >
        {isUser ? (
          <User className="h-4 w-4" />
        ) : isTool ? (
          <Wrench className="h-4 w-4" />
        ) : (
          <Bot className="h-4 w-4" />
        )}
      </div>

      {/* Message content */}
      <div
        className={cn(
          "flex flex-col overflow-hidden",
          isUser 
            ? "items-end max-w-[85%]" 
            : "items-start min-w-0 flex-1"
        )}
      >
        {/* Quoted text (user message) */}
        {isUser && message.quotedText && (
          <QuotedTextDisplay quotedText={message.quotedText} />
        )}

        {/* Image preview */}
        {message.images && message.images.length > 0 && (
          <div className="mb-2 flex flex-wrap gap-2">
            {message.images.map((img, index) => (
              <img
                key={index}
                src={img.startsWith("data:") ? img : `data:image/png;base64,${img}`}
                alt={t("message.uploadedImage")}
                className="max-h-32 max-w-32 rounded-md object-cover"
              />
            ))}
          </div>
        )}

        {/* Render in contentBlocks order (AI messages) */}
        {!isUser && hasContentBlocks ? (
          <div className="w-full space-y-1 overflow-hidden">
            {message.contentBlocks!.map((block, index) => (
              <ContentBlockRenderer key={index} block={block} index={index} />
            ))}
          </div>
        ) : (
          <>
            {/* Legacy structure compatibility: thinking content */}
            {!isUser && message.thinking && !hasContentBlocks && (
              <ThinkingDisplay thinking={message.thinking} />
            )}

            {/* Text content */}
            {message.content && (
              <div
                className={cn(
                  "rounded-2xl px-3 py-2 overflow-hidden",
                  isUser
                    ? "bg-primary text-primary-foreground rounded-br-md"
                    : "bg-muted/50 text-foreground w-full rounded-bl-md"
                )}
              >
                {isUser ? (
                  <p className="whitespace-pre-wrap break-words text-sm">{message.content}</p>
                ) : !hasContentBlocks ? (
                  <div className="prose prose-sm dark:prose-invert max-w-none prose-p:my-1 prose-pre:my-2 prose-pre:p-0 prose-pre:bg-transparent break-words">
                    <ReactMarkdown 
                      remarkPlugins={[remarkGfm]}
                      components={{
                        pre: ({ children }) => (
                          <pre className="overflow-x-auto rounded-lg p-4 text-sm my-2 max-w-full" style={{ backgroundColor: '#18181b' }}>
                            {children}
                          </pre>
                        ),
                        code: ({ className, children, ...props }) => {
                          const match = /language-(\w+)/.exec(className || '')
                          if (!className && !match) {
                            return (
                              <code className="rounded px-1.5 py-0.5 text-xs text-zinc-200 font-mono break-all" style={{ backgroundColor: '#27272a' }} {...props}>
                                {children}
                              </code>
                            )
                          }
                          return (
                            <code className="text-sm text-zinc-100 font-mono block" {...props}>
                              {children}
                            </code>
                          )
                        },
                      }}
                    >
                      {message.content}
                    </ReactMarkdown>
                  </div>
                ) : null}
              </div>
            )}

            {/* Legacy structure compatibility: tool calls */}
            {!hasContentBlocks && message.toolCalls && message.toolCalls.length > 0 && (
              <div className="mt-1 w-full">
                {message.toolCalls.map((toolCall) => (
                  <ToolCallDisplay key={toolCall.id} toolCall={toolCall} />
                ))}
              </div>
            )}
          </>
        )}

        {/* Tool result */}
        {message.toolResult && (
          <div className="mt-1 w-full">
            <ToolResultDisplay toolResult={message.toolResult} />
          </div>
        )}

        {/* Timestamp and token statistics */}
        <div className={cn(
          "mt-1 flex items-center gap-2 text-xs text-muted-foreground",
          isUser ? "flex-row-reverse" : "flex-row"
        )}>
          <span>{new Date(message.timestamp).toLocaleTimeString()}</span>
          {message.tokenUsage && (
            <span className="flex items-center gap-1">
              <Coins className="h-3 w-3" />
              {message.tokenUsage.inputTokens + message.tokenUsage.outputTokens} {t("message.tokens")}
              <span className="text-muted-foreground/70">
                ({t("message.inputTokens")}: {message.tokenUsage.inputTokens}, {t("message.outputTokens")}: {message.tokenUsage.outputTokens})
              </span>
            </span>
          )}
        </div>
      </div>
    </div>
  )
}
