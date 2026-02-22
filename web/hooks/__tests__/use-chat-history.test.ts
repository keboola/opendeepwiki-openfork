/**
 * Chat history property tests
 *
 * Property 2: Chat history completeness
 * Validates: Requirements 8.1, 8.2, 8.3
 *
 * Feature: doc-chat-assistant, Property 2: Chat history completeness
 */
import { describe, it, expect, beforeEach } from 'vitest'
import { renderHook, act } from '@testing-library/react'
import * as fc from 'fast-check'
import { 
  useChatHistory, 
  ChatMessage, 
  NewChatMessage,
  ToolCall,
  ToolResult 
} from '../use-chat-history'

// Generate random tool calls
const toolCallArb = fc.record({
  id: fc.string({ minLength: 1, maxLength: 20 }),
  name: fc.string({ minLength: 1, maxLength: 50 }),
  arguments: fc.dictionary(fc.string(), fc.jsonValue()),
})

// Generate random tool results
const toolResultArb = fc.record({
  toolCallId: fc.string({ minLength: 1, maxLength: 20 }),
  result: fc.string(),
  isError: fc.boolean(),
})

// Generate random message roles
const roleArb = fc.constantFrom('user', 'assistant', 'tool') as fc.Arbitrary<'user' | 'assistant' | 'tool'>

// Generate random new messages
const newMessageArb: fc.Arbitrary<NewChatMessage> = fc.record({
  role: roleArb,
  content: fc.string(),
  images: fc.option(fc.array(fc.base64String(), { maxLength: 3 }), { nil: undefined }),
  toolCalls: fc.option(fc.array(toolCallArb, { maxLength: 3 }), { nil: undefined }),
  toolResult: fc.option(toolResultArb, { nil: undefined }),
})

describe('useChatHistory Property Tests', () => {
  /**
   * Property 2: Chat history completeness
   *
   * For any chat session, message history must contain all user messages, AI replies,
   * tool calls, and tool results, and the complete history must be passed when sending new messages
   */
  describe('Property 2: Chat history completeness', () => {
    it('added messages should be fully preserved in the history', () => {
      fc.assert(
        fc.property(
          fc.array(newMessageArb, { minLength: 1, maxLength: 20 }),
          (messages) => {
            const { result } = renderHook(() => useChatHistory())
            
            // Add all messages
            const addedIds: string[] = []
            messages.forEach(msg => {
              act(() => {
                const id = result.current.addMessage(msg)
                addedIds.push(id)
              })
            })

            // Verify message count
            expect(result.current.messages.length).toBe(messages.length)

            // Verify content completeness of each message
            result.current.messages.forEach((storedMsg, index) => {
              const originalMsg = messages[index]
              expect(storedMsg.role).toBe(originalMsg.role)
              expect(storedMsg.content).toBe(originalMsg.content)
              expect(storedMsg.images).toEqual(originalMsg.images)
              expect(storedMsg.toolCalls).toEqual(originalMsg.toolCalls)
              expect(storedMsg.toolResult).toEqual(originalMsg.toolResult)
              expect(storedMsg.id).toBe(addedIds[index])
              expect(typeof storedMsg.timestamp).toBe('number')
            })
            
            return true
          }
        ),
        { numRuns: 100 }
      )
    })

    it('message history should contain all message types (user, assistant, tool)', () => {
      fc.assert(
        fc.property(
          fc.array(roleArb, { minLength: 1, maxLength: 30 }),
          (roles) => {
            const { result } = renderHook(() => useChatHistory())
            
            // Add a message for each role
            roles.forEach(role => {
              act(() => {
                result.current.addMessage({
                  role,
                  content: `Message from ${role}`,
                })
              })
            })
            
            // Verify all role messages are preserved
            const storedRoles = result.current.messages.map(m => m.role)
            expect(storedRoles).toEqual(roles)
            
            return true
          }
        ),
        { numRuns: 100 }
      )
    })

    it('tool calls and tool results should be correctly associated', () => {
      fc.assert(
        fc.property(
          toolCallArb,
          toolResultArb,
          (toolCall, toolResult) => {
            const { result } = renderHook(() => useChatHistory())
            
            // Add assistant message with tool call
            act(() => {
              result.current.addMessage({
                role: 'assistant',
                content: 'Using tool...',
                toolCalls: [toolCall],
              })
            })
            
            // Add tool result message
            act(() => {
              result.current.addMessage({
                role: 'tool',
                content: toolResult.result,
                toolResult: { ...toolResult, toolCallId: toolCall.id },
              })
            })
            
            // Verify both tool call and result are preserved
            expect(result.current.messages.length).toBe(2)
            expect(result.current.messages[0].toolCalls?.[0]).toEqual(toolCall)
            expect(result.current.messages[1].toolResult?.toolCallId).toBe(toolCall.id)
            
            return true
          }
        ),
        { numRuns: 100 }
      )
    })

    it('updating a message should keep other fields unchanged', () => {
      fc.assert(
        fc.property(
          newMessageArb,
          fc.string(),
          (originalMsg, newContent) => {
            const { result } = renderHook(() => useChatHistory())
            
            // Add message
            let msgId: string = ''
            act(() => {
              msgId = result.current.addMessage(originalMsg)
            })
            
            const originalTimestamp = result.current.messages[0].timestamp
            
            // Update message content
            act(() => {
              result.current.updateMessage(msgId, { content: newContent })
            })
            
            // Verify only content was updated, other fields remain unchanged
            const updatedMsg = result.current.messages[0]
            expect(updatedMsg.content).toBe(newContent)
            expect(updatedMsg.role).toBe(originalMsg.role)
            expect(updatedMsg.images).toEqual(originalMsg.images)
            expect(updatedMsg.toolCalls).toEqual(originalMsg.toolCalls)
            expect(updatedMsg.toolResult).toEqual(originalMsg.toolResult)
            expect(updatedMsg.id).toBe(msgId)
            expect(updatedMsg.timestamp).toBe(originalTimestamp)
            
            return true
          }
        ),
        { numRuns: 100 }
      )
    })

    it('message list should be empty after clearing history', () => {
      fc.assert(
        fc.property(
          fc.array(newMessageArb, { minLength: 1, maxLength: 20 }),
          (messages) => {
            const { result } = renderHook(() => useChatHistory())
            
            // Add message
            messages.forEach(msg => {
              act(() => {
                result.current.addMessage(msg)
              })
            })
            
            expect(result.current.messages.length).toBe(messages.length)
            
            // Clear history
            act(() => {
              result.current.clearHistory()
            })
            
            // Verify history is empty
            expect(result.current.messages.length).toBe(0)
            
            return true
          }
        ),
        { numRuns: 100 }
      )
    })

    it('each message should have a unique ID', () => {
      fc.assert(
        fc.property(
          fc.array(newMessageArb, { minLength: 2, maxLength: 50 }),
          (messages) => {
            const { result } = renderHook(() => useChatHistory())
            
            // Add all messages
            const ids: string[] = []
            messages.forEach(msg => {
              act(() => {
                const id = result.current.addMessage(msg)
                ids.push(id)
              })
            })
            
            // Verify all IDs are unique
            const uniqueIds = new Set(ids)
            expect(uniqueIds.size).toBe(ids.length)
            
            return true
          }
        ),
        { numRuns: 100 }
      )
    })
  })
})
