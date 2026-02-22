"use client"

import * as React from "react"
import { useTranslations } from "next-intl"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { ModelConfig } from "@/lib/chat-api"

/**
 * Model selector props
 */
export interface ModelSelectorProps {
  /** Available models list */
  models: ModelConfig[]
  /** Currently selected model ID */
  selectedModelId: string
  /** Model change callback */
  onModelChange: (modelId: string) => void
  /** Whether disabled */
  disabled?: boolean
}

/**
 * Model selector component
 *
 * Requirements: 2.2, 3.1, 3.2, 3.3
 */
export function ModelSelector({
  models,
  selectedModelId,
  onModelChange,
  disabled = false,
}: ModelSelectorProps) {
  const t = useTranslations("chat")
  const enabledModels = models.filter(m => m.isEnabled)

  if (enabledModels.length === 0) {
    return (
      <div className="text-sm text-muted-foreground">
        {t("model.noModels")}
      </div>
    )
  }

  return (
    <Select
      value={selectedModelId}
      onValueChange={onModelChange}
      disabled={disabled}
    >
      <SelectTrigger className="w-[180px] h-8 text-sm">
        <SelectValue placeholder={t("model.selector")} />
      </SelectTrigger>
      <SelectContent>
        {enabledModels.map((model) => (
          <SelectItem key={model.id} value={model.id}>
            <span className="flex items-center gap-2">
              <span>{model.name}</span>
              <span className="text-xs text-muted-foreground">
                ({model.provider})
              </span>
            </span>
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  )
}
