"use client"

import * as React from "react"
import { useTranslations } from "next-intl"
import { MessageCircle, X } from "lucide-react"
import { cn } from "@/lib/utils"

/**
 * Floating ball component props
 */
export interface FloatingBallProps {
  /** Whether enabled */
  enabled: boolean
  /** Custom icon URL */
  iconUrl?: string
  /** Whether currently expanded */
  isOpen: boolean
  /** Toggle expand/collapse callback */
  onToggle: () => void
  /** Custom class name */
  className?: string
}

/**
 * Floating ball component
 *
 * A circular button fixed at the bottom-right of the page, expands the chat panel on click
 *
 * Requirements: 1.1, 1.2, 1.3, 1.4, 1.5
 */
export function FloatingBall({
  enabled,
  iconUrl,
  isOpen,
  onToggle,
  className,
}: FloatingBallProps) {
  const t = useTranslations("chat")
  
  // If feature is not enabled, don't show floating ball
  if (!enabled) {
    return null
  }

  return (
    <button
      type="button"
      onClick={onToggle}
      className={cn(
        // Base styles
        "fixed z-50 flex items-center justify-center",
        "w-14 h-14 rounded-full",
        "bg-primary text-primary-foreground",
        "shadow-lg",
        // Position: bottom-right
        "right-6 bottom-6",
        // Transition animation
        "transition-all duration-200 ease-in-out",
        // Hover effect: scale up 1.1x
        "hover:scale-110",
        // Click effect
        "active:scale-95",
        // Focus styles
        "focus:outline-none focus:ring-2 focus:ring-primary focus:ring-offset-2",
        className
      )}
      aria-label={isOpen ? t("panel.close") : t("assistant.title")}
      aria-expanded={isOpen}
    >
      {isOpen ? (
        // Expanded state shows close icon
        <X className="h-6 w-6" />
      ) : iconUrl ? (
        // Custom icon
        <img
          src={iconUrl}
          alt={t("assistant.title")}
          className="h-8 w-8 rounded-full object-cover"
        />
      ) : (
        // Default message icon
        <MessageCircle className="h-6 w-6" />
      )}
    </button>
  )
}
