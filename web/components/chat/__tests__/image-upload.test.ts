/**
 * Image encoding property tests
 *
 * Property 6: Image encoding correctness
 * Validates: Requirements 4.3, 4.4
 *
 * Feature: doc-chat-assistant, Property 6: Image encoding correctness
 */
import { describe, it, expect } from 'vitest'
import * as fc from 'fast-check'
import {
  validateImageFile,
  validateBase64Image,
  getMimeTypeFromBase64,
  SUPPORTED_IMAGE_TYPES,
  MAX_IMAGE_SIZE,
} from '../image-upload'

// Supported MIME types
const supportedMimeTypes = ['image/png', 'image/jpeg', 'image/gif', 'image/webp'] as const

// Unsupported MIME types
const unsupportedMimeTypes = [
  'image/bmp',
  'image/tiff',
  'image/svg+xml',
  'application/pdf',
  'text/plain',
  'video/mp4',
]

// Generate valid Base64 image strings
const validBase64ImageArb = fc.constantFrom(...supportedMimeTypes).map(mimeType => {
  // Generate a minimal valid image data
  const minimalImageData = 'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg=='
  return `data:${mimeType};base64,${minimalImageData}`
})

// Generate invalid Base64 image strings (unsupported formats)
const invalidBase64ImageArb = fc.constantFrom(...unsupportedMimeTypes).map(mimeType => {
  const minimalData = 'SGVsbG8gV29ybGQ='
  return `data:${mimeType};base64,${minimalData}`
})

// Generate valid file size (less than 10MB)
const validFileSizeArb = fc.integer({ min: 1, max: MAX_IMAGE_SIZE - 1 })

// Generate invalid file size (greater than 10MB)
const invalidFileSizeArb = fc.integer({ min: MAX_IMAGE_SIZE + 1, max: MAX_IMAGE_SIZE * 2 })

// Create mock File object
function createMockFile(type: string, size: number): File {
  const content = new Uint8Array(size)
  const blob = new Blob([content], { type })
  return new File([blob], 'test-image', { type })
}

describe('ImageUpload Property Tests', () => {
  /**
   * Property 6: Image encoding correctness
   *
   * For any message containing images, images must be correctly encoded in Base64 format,
   * and the format must be one of PNG, JPG, GIF or WebP
   */
  describe('Property 6: Image encoding correctness', () => {
    it('supported image formats should pass validation', () => {
      fc.assert(
        fc.property(
          fc.constantFrom(...supportedMimeTypes),
          validFileSizeArb,
          (mimeType, size) => {
            const file = createMockFile(mimeType, size)
            const result = validateImageFile(file)
            
            expect(result.valid).toBe(true)
            expect(result.error).toBeUndefined()
            
            return true
          }
        ),
        { numRuns: 100 }
      )
    })

    it('unsupported image formats should be rejected', () => {
      fc.assert(
        fc.property(
          fc.constantFrom(...unsupportedMimeTypes),
          validFileSizeArb,
          (mimeType, size) => {
            const file = createMockFile(mimeType, size)
            const result = validateImageFile(file)
            
            expect(result.valid).toBe(false)
            expect(result.error).toBeDefined()
            expect(result.error).toContain('Unsupported image format')

            return true
          }
        ),
        { numRuns: 100 }
      )
    })

    it('images exceeding size limit should be rejected', () => {
      fc.assert(
        fc.property(
          fc.constantFrom(...supportedMimeTypes),
          invalidFileSizeArb,
          (mimeType, size) => {
            const file = createMockFile(mimeType, size)
            const result = validateImageFile(file)
            
            expect(result.valid).toBe(false)
            expect(result.error).toBeDefined()
            expect(result.error).toContain('exceeds limit')
            
            return true
          }
        ),
        { numRuns: 100 }
      )
    })

    it('valid Base64 images should pass validation', () => {
      fc.assert(
        fc.property(
          validBase64ImageArb,
          (base64) => {
            const result = validateBase64Image(base64)
            
            expect(result.valid).toBe(true)
            expect(result.error).toBeUndefined()
            
            return true
          }
        ),
        { numRuns: 100 }
      )
    })

    it('Base64 images with invalid format should be rejected', () => {
      fc.assert(
        fc.property(
          invalidBase64ImageArb,
          (base64) => {
            const result = validateBase64Image(base64)
            
            expect(result.valid).toBe(false)
            expect(result.error).toBeDefined()
            expect(result.error).toContain('Unsupported image format')

            return true
          }
        ),
        { numRuns: 100 }
      )
    })

    it('should correctly extract MIME type from Base64 images', () => {
      fc.assert(
        fc.property(
          fc.constantFrom(...supportedMimeTypes),
          (mimeType) => {
            const base64 = `data:${mimeType};base64,SGVsbG8=`
            const extractedType = getMimeTypeFromBase64(base64)
            
            expect(extractedType).toBe(mimeType)
            
            return true
          }
        ),
        { numRuns: 100 }
      )
    })

    it('invalid Base64 strings should return null', () => {
      fc.assert(
        fc.property(
          fc.string().filter(s => !s.startsWith('data:')),
          (invalidString) => {
            const result = getMimeTypeFromBase64(invalidString)
            expect(result).toBeNull()
            return true
          }
        ),
        { numRuns: 100 }
      )
    })

    it('SUPPORTED_IMAGE_TYPES should contain all required formats', () => {
      const requiredTypes = ['image/png', 'image/jpeg', 'image/gif', 'image/webp']
      
      requiredTypes.forEach(type => {
        expect(SUPPORTED_IMAGE_TYPES).toContain(type)
      })
    })

    it('MAX_IMAGE_SIZE should be 10MB', () => {
      expect(MAX_IMAGE_SIZE).toBe(10 * 1024 * 1024)
    })
  })
})
