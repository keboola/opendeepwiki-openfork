"use client"

import * as React from "react"
import { useTranslations } from "next-intl"
import { ImagePlus, X } from "lucide-react"
import { cn } from "@/lib/utils"
import { Button } from "@/components/ui/button"
import {
  validateImageFile,
  fileToBase64,
  validateBase64Image,
  getMimeTypeFromBase64,
  SUPPORTED_IMAGE_TYPES,
  MAX_IMAGE_SIZE,
} from "@/lib/image-validation"

/**
 * Image upload component props
 */
export interface ImageUploadProps {
  /** List of uploaded images (Base64) */
  images: string[]
  /** Image change callback */
  onImagesChange: (images: string[]) => void
  /** Error callback */
  onError?: (error: string) => void
  /** Whether disabled */
  disabled?: boolean
  /** Maximum number of images */
  maxImages?: number
  /** Custom class name */
  className?: string
}

/**
 * Image upload component
 *
 * Supports PNG, JPG, GIF, WebP formats
 * Image size limit 10MB
 * Image preview functionality
 * Base64 encoding
 *
 * Requirements: 4.1, 4.2, 4.3, 4.4, 4.5
 */
export function ImageUpload({
  images,
  onImagesChange,
  onError,
  disabled = false,
  maxImages = 5,
  className,
}: ImageUploadProps) {
  const t = useTranslations("chat")
  const fileInputRef = React.useRef<HTMLInputElement>(null)

  // Handle file selection
  const handleFileSelect = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files
    if (!files || files.length === 0) return

    const newImages: string[] = []
    const errors: string[] = []

    for (const file of Array.from(files)) {
      // Check if maximum count exceeded
      if (images.length + newImages.length >= maxImages) {
        errors.push(t("image.supportedFormats", { maxImages }))
        break
      }

      // Validate file
      const validation = validateImageFile(file)
      if (!validation.valid) {
        errors.push(validation.error!)
        continue
      }

      try {
        const base64 = await fileToBase64(file)
        newImages.push(base64)
      } catch (err) {
        errors.push(t("image.readFailed"))
      }
    }

    // Update image list
    if (newImages.length > 0) {
      onImagesChange([...images, ...newImages])
    }

    // Report errors
    if (errors.length > 0 && onError) {
      onError(errors.join("; "))
    }

    // Clear input to allow re-selecting the same file
    e.target.value = ""
  }

  // Remove image
  const handleRemove = (index: number) => {
    onImagesChange(images.filter((_, i) => i !== index))
  }

  // Trigger file selection
  const handleClick = () => {
    fileInputRef.current?.click()
  }

  return (
    <div className={cn("flex flex-col gap-2", className)}>
      {/* Image preview */}
      {images.length > 0 && (
        <div className="flex flex-wrap gap-2">
          {images.map((img, index) => (
            <div key={index} className="relative group">
              <img
                src={img}
                alt={t("image.preview", { index: index + 1 })}
                className="h-16 w-16 rounded-md object-cover border border-border"
              />
              <button
                type="button"
                onClick={() => handleRemove(index)}
                disabled={disabled}
                className={cn(
                  "absolute -right-1 -top-1 rounded-full p-0.5",
                  "bg-destructive text-destructive-foreground",
                  "opacity-0 group-hover:opacity-100 transition-opacity",
                  "focus:opacity-100",
                  disabled && "cursor-not-allowed"
                )}
                aria-label={t("image.remove", { index: index + 1 })}
              >
                <X className="h-3 w-3" />
              </button>
            </div>
          ))}
        </div>
      )}

      {/* Upload button */}
      <input
        ref={fileInputRef}
        type="file"
        accept={SUPPORTED_IMAGE_TYPES.join(",")}
        multiple
        className="hidden"
        onChange={handleFileSelect}
        disabled={disabled}
      />
      
      {images.length < maxImages && (
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={handleClick}
          disabled={disabled}
          className="w-fit"
        >
          <ImagePlus className="mr-2 h-4 w-4" />
          {t("image.upload")}
        </Button>
      )}

      {/* Hint information */}
      <p className="text-xs text-muted-foreground">
        {t("image.supportedFormats", { maxImages })}
      </p>
    </div>
  )
}

// Export validation functions and constants for external use
export {
  validateImageFile,
  fileToBase64,
  validateBase64Image,
  getMimeTypeFromBase64,
  SUPPORTED_IMAGE_TYPES,
  MAX_IMAGE_SIZE,
} from "@/lib/image-validation"

