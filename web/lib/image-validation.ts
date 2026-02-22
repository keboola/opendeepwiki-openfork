/**
 * Image validation utility functions
 * Supports internationalized error messages
 */

export const SUPPORTED_IMAGE_TYPES = [
  "image/png",
  "image/jpeg",
  "image/gif",
  "image/webp",
] as const

export const MAX_IMAGE_SIZE = 10 * 1024 * 1024

/**
 * Validate image file
 * @param file File object
 * @param t Translation function
 */
export function validateImageFile(
  file: File,
  t?: (key: string, values?: Record<string, any>) => string
): { valid: boolean; error?: string } {
  // Check file type
  if (!SUPPORTED_IMAGE_TYPES.includes(file.type as typeof SUPPORTED_IMAGE_TYPES[number])) {
    return {
      valid: false,
      error: t
        ? t("chat.image.unsupportedFormat")
        : `Unsupported image format: ${file.type}. Only PNG, JPG, GIF, WebP are supported`,
    }
  }

  // Check file size
  if (file.size > MAX_IMAGE_SIZE) {
    const sizeMB = (file.size / (1024 * 1024)).toFixed(2)
    return {
      valid: false,
      error: t
        ? t("chat.image.sizeTooLarge")
        : `Image size (${sizeMB}MB) exceeds limit (10MB)`,
    }
  }

  return { valid: true }
}

/**
 * Convert file to Base64
 */
export function fileToBase64(file: File): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader()
    reader.onload = () => resolve(reader.result as string)
    reader.onerror = () => reject(new Error("Failed to read file"))
    reader.readAsDataURL(file)
  })
}

/**
 * Extract MIME type from Base64 string
 */
export function getMimeTypeFromBase64(base64: string): string | null {
  const match = base64.match(/^data:([^;]+);base64,/)
  return match ? match[1] : null
}

/**
 * Validate Base64 image format
 * @param base64 Base64 string
 * @param t Translation function
 */
export function validateBase64Image(
  base64: string,
  t?: (key: string) => string
): { valid: boolean; error?: string } {
  const mimeType = getMimeTypeFromBase64(base64)

  if (!mimeType) {
    return {
      valid: false,
      error: t ? t("chat.image.invalidFormat") : "Invalid Base64 image format",
    }
  }

  if (!SUPPORTED_IMAGE_TYPES.includes(mimeType as typeof SUPPORTED_IMAGE_TYPES[number])) {
    return {
      valid: false,
      error: t
        ? t("chat.image.unsupportedFormat")
        : `Unsupported image format: ${mimeType}. Only PNG, JPG, GIF, WebP are supported`,
    }
  }

  return { valid: true }
}
