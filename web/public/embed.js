/**
 * OpenDeepWiki Embed Script
 *
 * Embeds a chat assistant floating widget into external websites
 *
 * Usage:
 * <script 
 *   src="https://your-domain.com/embed.js"
 *   data-app-id="app_xxxxx"
 *   data-icon="https://example.com/icon.png"
 * ></script>
 * 
 * Requirements: 14.2, 14.3, 14.4, 14.7
 */
(function() {
  'use strict';

  // Get current script element
  var script = document.currentScript;
  if (!script) {
    console.error('[OpenDeepWiki] Cannot get script element');
    return;
  }

  // Read configuration attributes
  var appId = script.getAttribute('data-app-id');
  var iconUrl = script.getAttribute('data-icon');
  var position = script.getAttribute('data-position') || 'bottom-right';
  var theme = script.getAttribute('data-theme') || 'light';

  // Validate required parameters
  if (!appId) {
    console.error('[OpenDeepWiki] data-app-id is required');
    return;
  }

  // API base URL - extracted from script src
  var scriptSrc = script.src;
  var apiBaseUrl = scriptSrc.substring(0, scriptSrc.lastIndexOf('/'));
  // Remove /embed.js or similar path to get root URL
  apiBaseUrl = apiBaseUrl.replace(/\/public$/, '').replace(/\/$/, '');

  // Configuration object
  var config = {
    appId: appId,
    iconUrl: iconUrl,
    position: position,
    theme: theme,
    apiBaseUrl: apiBaseUrl
  };

  // State
  var state = {
    isOpen: false,
    isLoading: true,
    isEnabled: false,
    isResizing: false,
    startX: 0,
    startWidth: 400,
    appConfig: null,
    messages: [],
    selectedModel: null
  };

  // Style definitions
  var styles = {
    container: [
      'position: fixed',
      'z-index: 999999',
      'font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif'
    ].join(';'),
    backdrop: [
      'position: fixed',
      'top: 0',
      'left: 0',
      'right: 0',
      'bottom: 0',
      'background: rgba(0, 0, 0, 0.2)',
      'transition: opacity 0.3s ease',
      'z-index: 999998'
    ].join(';'),
    floatingBall: [
      'position: fixed',
      'width: 56px',
      'height: 56px',
      'border-radius: 50%',
      'background: linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
      'border: none',
      'cursor: pointer',
      'display: flex',
      'align-items: center',
      'justify-content: center',
      'box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15)',
      'transition: transform 0.2s ease, box-shadow 0.2s ease',
      'outline: none',
      'right: 24px',
      'bottom: 24px',
      'z-index: 999999'
    ].join(';'),
    floatingBallHover: 'transform: scale(1.1); box-shadow: 0 6px 20px rgba(0, 0, 0, 0.2);',
    panel: [
      'position: fixed',
      'top: 0',
      'right: 0',
      'width: 400px',
      'height: 100%',
      'max-width: 100vw',
      'background: #ffffff',
      'box-shadow: -4px 0 24px rgba(0, 0, 0, 0.15)',
      'display: flex',
      'flex-direction: column',
      'overflow: hidden',
      'transition: transform 0.3s ease',
      'z-index: 999999'
    ].join(';'),
    panelDark: 'background: #1a1a2e; color: #ffffff;',
    header: [
      'display: flex',
      'align-items: center',
      'justify-content: space-between',
      'padding: 16px',
      'border-bottom: 1px solid #e5e7eb',
      'background: #f9fafb'
    ].join(';'),
    headerDark: 'background: #16213e; border-bottom-color: #374151;',
    messagesContainer: [
      'flex: 1',
      'overflow-y: auto',
      'overflow-x: hidden',
      'padding: 16px',
      'display: flex',
      'flex-direction: column',
      'gap: 12px',
      'min-height: 0',
      'scrollbar-width: thin',
      'scrollbar-color: #d1d5db transparent'
    ].join(';'),
    inputContainer: [
      'padding: 16px',
      'border-top: 1px solid #e5e7eb',
      'display: flex',
      'gap: 8px',
      'align-items: flex-end'
    ].join(';'),
    inputContainerDark: 'border-top-color: #374151;',
    textarea: [
      'flex: 1',
      'min-height: 40px',
      'max-height: 120px',
      'padding: 10px 12px',
      'border: 1px solid #d1d5db',
      'border-radius: 8px',
      'resize: none',
      'font-size: 14px',
      'line-height: 1.5',
      'outline: none',
      'transition: border-color 0.2s ease'
    ].join(';'),
    textareaDark: 'background: #1e293b; border-color: #475569; color: #ffffff;',
    sendButton: [
      'width: 40px',
      'height: 40px',
      'border-radius: 8px',
      'background: linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
      'border: none',
      'cursor: pointer',
      'display: flex',
      'align-items: center',
      'justify-content: center',
      'transition: opacity 0.2s ease'
    ].join(';'),
    userMessage: [
      'align-self: flex-end',
      'max-width: 80%',
      'padding: 10px 14px',
      'background: linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
      'color: #ffffff',
      'border-radius: 16px 16px 4px 16px',
      'font-size: 14px',
      'line-height: 1.5',
      'word-wrap: break-word'
    ].join(';'),
    assistantMessage: [
      'align-self: flex-start',
      'max-width: 80%',
      'padding: 10px 14px',
      'background: #f3f4f6',
      'color: #1f2937',
      'border-radius: 16px 16px 16px 4px',
      'font-size: 14px',
      'line-height: 1.5',
      'word-wrap: break-word'
    ].join(';'),
    assistantMessageDark: 'background: #374151; color: #f3f4f6;',
    welcomeMessage: [
      'text-align: center',
      'color: #6b7280',
      'padding: 40px 20px'
    ].join(';'),
    errorMessage: [
      'padding: 12px 16px',
      'background: #fef2f2',
      'color: #dc2626',
      'border-radius: 8px',
      'font-size: 14px',
      'margin: 8px 16px'
    ].join(';'),
    loadingDots: [
      'display: inline-flex',
      'gap: 4px'
    ].join(';'),
    loadingDot: [
      'width: 8px',
      'height: 8px',
      'background: #9ca3af',
      'border-radius: 50%',
      'animation: odw-bounce 1.4s infinite ease-in-out both'
    ].join(';'),
    modelSelector: [
      'padding: 6px 12px',
      'border: 1px solid #d1d5db',
      'border-radius: 6px',
      'font-size: 12px',
      'background: #ffffff',
      'cursor: pointer',
      'outline: none'
    ].join(';'),
    modelSelectorDark: 'background: #1e293b; border-color: #475569; color: #ffffff;'
  };

  // Icon SVGs
  var icons = {
    chat: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"></path></svg>',
    close: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg>',
    send: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="22" y1="2" x2="11" y2="13"></line><polygon points="22 2 15 22 11 13 2 9 22 2"></polygon></svg>'
  };

  // Inject CSS animations
  function injectStyles() {
    var styleEl = document.createElement('style');
    styleEl.textContent = [
      '@keyframes odw-bounce {',
      '  0%, 80%, 100% { transform: scale(0); }',
      '  40% { transform: scale(1); }',
      '}',
      '.odw-dot-1 { animation-delay: -0.32s; }',
      '.odw-dot-2 { animation-delay: -0.16s; }',
      '.odw-dot-3 { animation-delay: 0s; }',
      '#odw-messages::-webkit-scrollbar { width: 6px; }',
      '#odw-messages::-webkit-scrollbar-track { background: transparent; }',
      '#odw-messages::-webkit-scrollbar-thumb { background: #d1d5db; border-radius: 3px; }',
      '#odw-messages::-webkit-scrollbar-thumb:hover { background: #9ca3af; }',
      '@media (max-width: 480px) {',
      '  #odw-panel { width: 100% !important; }',
      '}'
    ].join('\n');
    document.head.appendChild(styleEl);
  }


  // Generate unique ID
  function generateId() {
    return 'odw-' + Math.random().toString(36).substr(2, 9);
  }

  // Create DOM element
  function createElement(tag, attrs, children) {
    var el = document.createElement(tag);
    if (attrs) {
      Object.keys(attrs).forEach(function(key) {
        if (key === 'style') {
          el.style.cssText = attrs[key];
        } else if (key === 'className') {
          el.className = attrs[key];
        } else if (key.startsWith('on')) {
          el.addEventListener(key.substring(2).toLowerCase(), attrs[key]);
        } else {
          el.setAttribute(key, attrs[key]);
        }
      });
    }
    if (children) {
      if (typeof children === 'string') {
        el.innerHTML = children;
      } else if (Array.isArray(children)) {
        children.forEach(function(child) {
          if (child) el.appendChild(child);
        });
      } else {
        el.appendChild(children);
      }
    }
    return el;
  }

  // Validate configuration and get app info
  function validateAndGetConfig(callback) {
    var url = config.apiBaseUrl + '/api/v1/embed/config?appId=' + encodeURIComponent(config.appId);
    
    fetch(url, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json'
      }
    })
    .then(function(response) {
      return response.json();
    })
    .then(function(data) {
      if (data.valid) {
        state.isEnabled = true;
        state.appConfig = data;
        state.selectedModel = data.defaultModel || (data.availableModels && data.availableModels[0]);
        callback(null, data);
      } else {
        console.error('[OpenDeepWiki] Configuration validation failed:', data.errorMessage);
        callback(new Error(data.errorMessage || 'Configuration validation failed'));
      }
    })
    .catch(function(error) {
      console.error('[OpenDeepWiki] Failed to get configuration:', error);
      callback(error);
    });
  }

  // SSE streaming chat
  function streamChat(messages, onContent, onDone, onError) {
    var url = config.apiBaseUrl + '/api/v1/embed/stream';
    
    var requestBody = {
      appId: config.appId,
      messages: messages.map(function(msg) {
        return {
          role: msg.role,
          content: msg.content
        };
      }),
      modelId: state.selectedModel
    };

    fetch(url, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(requestBody)
    })
    .then(function(response) {
      if (!response.ok) {
        throw new Error('Request failed: ' + response.status);
      }
      
      var reader = response.body.getReader();
      var decoder = new TextDecoder();
      var buffer = '';

      function processStream() {
        reader.read().then(function(result) {
          if (result.done) {
            onDone();
            return;
          }

          buffer += decoder.decode(result.value, { stream: true });
          var lines = buffer.split('\n');
          buffer = lines.pop() || '';

          lines.forEach(function(line) {
            line = line.trim();
            if (!line) return;

            // Parse SSE events
            if (line.startsWith('event: ')) {
              // Event type line, skip
              return;
            }
            
            if (line.startsWith('data: ')) {
              var dataStr = line.substring(6);
              try {
                var event = JSON.parse(dataStr);
                if (event.type === 'content') {
                  onContent(event.data);
                } else if (event.type === 'done') {
                  // Done event will be handled when stream ends
                } else if (event.type === 'error') {
                  onError(new Error(event.data.message || 'Chat failed'));
                }
              } catch (e) {
                // Might be plain text content
                onContent(dataStr);
              }
            }
          });

          processStream();
        }).catch(function(error) {
          onError(error);
        });
      }

      processStream();
    })
    .catch(function(error) {
      onError(error);
    });
  }

  // Render floating ball
  function renderFloatingBall(container) {
    var ballStyle = styles.floatingBall;

    var iconContent;
    if (config.iconUrl) {
      iconContent = '<img src="' + config.iconUrl + '" alt="Chat" style="width: 32px; height: 32px; border-radius: 50%; object-fit: cover;">';
    } else {
      iconContent = '<span style="color: white;">' + icons.chat + '</span>';
    }

    var ball = createElement('button', {
      id: 'odw-floating-ball',
      style: ballStyle,
      'aria-label': 'Open chat assistant',
      onClick: function() {
        togglePanel();
      },
      onMouseenter: function() {
        this.style.cssText = ballStyle + ';' + styles.floatingBallHover;
      },
      onMouseleave: function() {
        this.style.cssText = ballStyle;
      }
    }, iconContent);

    container.appendChild(ball);
    return ball;
  }

  // Render backdrop overlay
  function renderBackdrop(container) {
    var backdrop = createElement('div', {
      id: 'odw-backdrop',
      style: styles.backdrop + '; opacity: 0; pointer-events: none;',
      onClick: function() {
        togglePanel();
      }
    });
    container.appendChild(backdrop);
    return backdrop;
  }

  // Render chat panel
  function renderPanel(container) {
    var isDark = config.theme === 'dark';
    var panelStyle = styles.panel;
    if (isDark) {
      panelStyle += ';' + styles.panelDark;
    }
    // Initial state: hidden off-screen to the right
    panelStyle += '; transform: translateX(100%);';

    var panel = createElement('div', {
      id: 'odw-panel',
      style: panelStyle
    });

    // Drag handle for resizing width
    var resizeHandle = createElement('div', {
      id: 'odw-resize-handle',
      style: [
        'position: absolute',
        'left: 0',
        'top: 0',
        'width: 6px',
        'height: 100%',
        'cursor: ew-resize',
        'background: transparent',
        'transition: background 0.2s ease',
        'z-index: 10'
      ].join(';'),
      onMouseenter: function() {
        this.style.background = 'rgba(102, 126, 234, 0.3)';
      },
      onMouseleave: function() {
        if (!state.isResizing) {
          this.style.background = 'transparent';
        }
      },
      onMousedown: function(e) {
        e.preventDefault();
        state.isResizing = true;
        state.startX = e.clientX;
        state.startWidth = panel.offsetWidth;
        this.style.background = 'rgba(102, 126, 234, 0.5)';

        document.addEventListener('mousemove', handleResize);
        document.addEventListener('mouseup', stopResize);
      }
    });

    function handleResize(e) {
      if (!state.isResizing) return;
      var diff = state.startX - e.clientX;
      var newWidth = Math.min(Math.max(state.startWidth + diff, 320), window.innerWidth * 0.8);
      panel.style.width = newWidth + 'px';
    }

    function stopResize() {
      state.isResizing = false;
      var handle = document.getElementById('odw-resize-handle');
      if (handle) {
        handle.style.background = 'transparent';
      }
      document.removeEventListener('mousemove', handleResize);
      document.removeEventListener('mouseup', stopResize);
    }

    panel.appendChild(resizeHandle);

    // Header
    var headerStyle = styles.header;
    if (isDark) headerStyle += styles.headerDark;
    
    var header = createElement('div', { style: headerStyle }, [
      createElement('div', { style: 'display: flex; align-items: center; gap: 12px;' }, [
        createElement('span', { style: 'font-weight: 600; font-size: 16px;' }, state.appConfig ? state.appConfig.appName || 'Chat Assistant' : 'Chat Assistant'),
        renderModelSelector()
      ]),
      createElement('button', {
        style: 'background: none; border: none; cursor: pointer; padding: 4px; color: inherit;',
        onClick: function() { togglePanel(); }
      }, icons.close)
    ]);
    panel.appendChild(header);

    // Messages container
    var messagesContainer = createElement('div', {
      id: 'odw-messages',
      style: styles.messagesContainer
    });
    
    // Welcome message
    messagesContainer.appendChild(createElement('div', {
      style: styles.welcomeMessage
    }, [
      createElement('div', { style: 'font-size: 24px; margin-bottom: 8px;' }, '👋'),
      createElement('div', { style: 'font-weight: 500; margin-bottom: 4px;' }, 'Hello!'),
      createElement('div', { style: 'font-size: 14px;' }, 'How can I help you?')
    ]));
    
    panel.appendChild(messagesContainer);

    // Input area
    var inputStyle = styles.inputContainer;
    if (isDark) inputStyle += styles.inputContainerDark;
    
    var textareaStyle = styles.textarea;
    if (isDark) textareaStyle += styles.textareaDark;

    var textarea = createElement('textarea', {
      id: 'odw-input',
      style: textareaStyle,
      placeholder: 'Type a message...',
      rows: '1',
      onKeydown: function(e) {
        if (e.key === 'Enter' && !e.shiftKey) {
          e.preventDefault();
          sendMessage();
        }
      },
      onInput: function() {
        this.style.height = 'auto';
        this.style.height = Math.min(this.scrollHeight, 120) + 'px';
      }
    });

    var sendBtn = createElement('button', {
      id: 'odw-send-btn',
      style: styles.sendButton,
      onClick: function() { sendMessage(); }
    }, '<span style="color: white;">' + icons.send + '</span>');

    var inputContainer = createElement('div', { style: inputStyle }, [textarea, sendBtn]);
    panel.appendChild(inputContainer);

    container.appendChild(panel);
    return panel;
  }

  // Render model selector
  function renderModelSelector() {
    if (!state.appConfig || !state.appConfig.availableModels || state.appConfig.availableModels.length <= 1) {
      return null;
    }

    var isDark = config.theme === 'dark';
    var selectorStyle = styles.modelSelector;
    if (isDark) selectorStyle += styles.modelSelectorDark;

    var select = createElement('select', {
      id: 'odw-model-selector',
      style: selectorStyle,
      onChange: function() {
        state.selectedModel = this.value;
      }
    });

    state.appConfig.availableModels.forEach(function(model) {
      var option = createElement('option', { value: model }, model);
      if (model === state.selectedModel) {
        option.selected = true;
      }
      select.appendChild(option);
    });

    return select;
  }


  // Toggle panel visibility
  function togglePanel() {
    state.isOpen = !state.isOpen;
    var panel = document.getElementById('odw-panel');
    var ball = document.getElementById('odw-floating-ball');
    var backdrop = document.getElementById('odw-backdrop');

    if (panel) {
      if (state.isOpen) {
        // Expand: slide in from right
        panel.style.transform = 'translateX(0)';
        // Focus input field
        setTimeout(function() {
          var input = document.getElementById('odw-input');
          if (input) input.focus();
        }, 300);
      } else {
        // Collapse: slide out to right
        panel.style.transform = 'translateX(100%)';
      }
    }

    if (backdrop) {
      if (state.isOpen) {
        backdrop.style.opacity = '1';
        backdrop.style.pointerEvents = 'auto';
      } else {
        backdrop.style.opacity = '0';
        backdrop.style.pointerEvents = 'none';
      }
    }

    if (ball) {
      ball.innerHTML = state.isOpen
        ? '<span style="color: white;">' + icons.close + '</span>'
        : (config.iconUrl
            ? '<img src="' + config.iconUrl + '" alt="Chat" style="width: 32px; height: 32px; border-radius: 50%; object-fit: cover;">'
            : '<span style="color: white;">' + icons.chat + '</span>');
      ball.setAttribute('aria-label', state.isOpen ? 'Close chat assistant' : 'Open chat assistant');
    }
  }

  // Add message to UI
  function addMessageToUI(role, content) {
    var messagesContainer = document.getElementById('odw-messages');
    if (!messagesContainer) return;

    // Remove welcome message
    var welcomeMsg = messagesContainer.querySelector('[style*="text-align: center"]');
    if (welcomeMsg) {
      welcomeMsg.remove();
    }

    var isDark = config.theme === 'dark';
    var messageStyle = role === 'user' ? styles.userMessage : styles.assistantMessage;
    if (role === 'assistant' && isDark) {
      messageStyle += styles.assistantMessageDark;
    }

    var messageEl = createElement('div', {
      style: messageStyle,
      'data-role': role
    }, escapeHtml(content));

    messagesContainer.appendChild(messageEl);
    messagesContainer.scrollTop = messagesContainer.scrollHeight;

    return messageEl;
  }

  // Update last assistant message
  function updateLastAssistantMessage(content) {
    var messagesContainer = document.getElementById('odw-messages');
    if (!messagesContainer) return;

    var messages = messagesContainer.querySelectorAll('[data-role="assistant"]');
    var lastMessage = messages[messages.length - 1];
    
    if (lastMessage) {
      lastMessage.innerHTML = formatMarkdown(content);
    }
  }

  // Show loading indicator
  function showLoading() {
    var messagesContainer = document.getElementById('odw-messages');
    if (!messagesContainer) return;

    var isDark = config.theme === 'dark';
    var messageStyle = styles.assistantMessage;
    if (isDark) messageStyle += styles.assistantMessageDark;

    var loadingEl = createElement('div', {
      id: 'odw-loading',
      style: messageStyle
    }, [
      createElement('div', { style: styles.loadingDots }, [
        createElement('span', { style: styles.loadingDot, className: 'odw-dot-1' }),
        createElement('span', { style: styles.loadingDot, className: 'odw-dot-2' }),
        createElement('span', { style: styles.loadingDot, className: 'odw-dot-3' })
      ])
    ]);

    messagesContainer.appendChild(loadingEl);
    messagesContainer.scrollTop = messagesContainer.scrollHeight;
  }

  // Hide loading indicator
  function hideLoading() {
    var loadingEl = document.getElementById('odw-loading');
    if (loadingEl) {
      loadingEl.remove();
    }
  }

  // Show error message
  function showError(message) {
    var messagesContainer = document.getElementById('odw-messages');
    if (!messagesContainer) return;

    var errorEl = createElement('div', {
      style: styles.errorMessage
    }, escapeHtml(message));

    messagesContainer.appendChild(errorEl);
    messagesContainer.scrollTop = messagesContainer.scrollHeight;

    // Auto-remove after 5 seconds
    setTimeout(function() {
      errorEl.remove();
    }, 5000);
  }

  // Send message
  function sendMessage() {
    var input = document.getElementById('odw-input');
    var sendBtn = document.getElementById('odw-send-btn');
    if (!input || !sendBtn) return;

    var content = input.value.trim();
    if (!content) return;

    // Disable input
    input.disabled = true;
    sendBtn.disabled = true;
    sendBtn.style.opacity = '0.5';

    // Add user message
    state.messages.push({ role: 'user', content: content });
    addMessageToUI('user', content);

    // Clear input field
    input.value = '';
    input.style.height = 'auto';

    // Show loading
    showLoading();

    // Prepare assistant message
    var assistantContent = '';
    addMessageToUI('assistant', '');

    // Send request
    streamChat(
      state.messages,
      function(chunk) {
        // Content callback
        hideLoading();
        assistantContent += chunk;
        updateLastAssistantMessage(assistantContent);
      },
      function() {
        // Done callback
        hideLoading();
        state.messages.push({ role: 'assistant', content: assistantContent });
        
        // Restore input
        input.disabled = false;
        sendBtn.disabled = false;
        sendBtn.style.opacity = '1';
        input.focus();
      },
      function(error) {
        // Error callback
        hideLoading();
        showError(error.message || 'Failed to send, please try again');
        
        // Remove empty assistant message
        var messagesContainer = document.getElementById('odw-messages');
        var messages = messagesContainer.querySelectorAll('[data-role="assistant"]');
        var lastMessage = messages[messages.length - 1];
        if (lastMessage && !lastMessage.textContent.trim()) {
          lastMessage.remove();
        }
        
        // Restore input
        input.disabled = false;
        sendBtn.disabled = false;
        sendBtn.style.opacity = '1';
        input.focus();
      }
    );
  }

  // HTML escape
  function escapeHtml(text) {
    var div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
  }

  // Simple Markdown formatting
  function formatMarkdown(text) {
    if (!text) return '';
    
    // Escape HTML
    text = escapeHtml(text);
    
    // Code blocks
    text = text.replace(/```(\w*)\n([\s\S]*?)```/g, function(match, lang, code) {
      return '<pre style="background: #1e293b; color: #e2e8f0; padding: 12px; border-radius: 6px; overflow-x: auto; font-size: 13px; margin: 8px 0;"><code>' + code + '</code></pre>';
    });
    
    // Inline code
    text = text.replace(/`([^`]+)`/g, '<code style="background: #e5e7eb; padding: 2px 6px; border-radius: 4px; font-size: 13px;">$1</code>');
    
    // Bold
    text = text.replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>');
    
    // Italic
    text = text.replace(/\*([^*]+)\*/g, '<em>$1</em>');
    
    // Links
    text = text.replace(/\[([^\]]+)\]\(([^)]+)\)/g, '<a href="$2" target="_blank" style="color: #667eea; text-decoration: underline;">$1</a>');
    
    // Line breaks
    text = text.replace(/\n/g, '<br>');
    
    return text;
  }

  // Initialize
  function init() {
    // Inject styles
    injectStyles();

    // Create container
    var container = createElement('div', {
      id: 'odw-container',
      style: styles.container
    });
    document.body.appendChild(container);

    // Validate configuration
    state.isLoading = true;
    validateAndGetConfig(function(error, appConfig) {
      state.isLoading = false;

      if (error) {
        console.error('[OpenDeepWiki] Initialization failed:', error.message);
        return;
      }

      // Render UI - backdrop first, then panel, then floating ball
      renderBackdrop(container);
      renderPanel(container);
      renderFloatingBall(container);

      console.log('[OpenDeepWiki] Initialized successfully');
    });
  }

  // Wait for DOM to be ready
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }

  // Expose API for external use
  window.OpenDeepWiki = {
    open: function() {
      if (!state.isOpen) togglePanel();
    },
    close: function() {
      if (state.isOpen) togglePanel();
    },
    toggle: function() {
      togglePanel();
    },
    sendMessage: function(content) {
      var input = document.getElementById('odw-input');
      if (input) {
        input.value = content;
        sendMessage();
      }
    }
  };

})();
