import { Component, EventEmitter, HostBinding, Input, Output, OnDestroy, OnChanges, SimpleChanges, inject, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { Apollo } from 'apollo-angular';
import { ChatMessage } from '../../../../graphql/graphql-gateway.service';
import { ON_CHAT_MESSAGE } from '../gql/onChatMessage.graphql';
import { SEND_CHAT_MESSAGE } from '../gql/sendChatMessage.graphql';
import { GET_CHAT_MESSAGES } from '../gql/getChatMessages.graphql';

@Component({
  selector: 'app-chat',
  templateUrl: './chat.component.html',
  styleUrl: './chat.component.scss',
  standalone: false
})
export class ChatComponent implements OnChanges, OnDestroy {
  @Input() gameId: string = '';
  @Input() currentUserId: string = '';
  @Input() currentUserName: string = '';
  @Input() collapsed = false;

  @Output() collapsedChange = new EventEmitter<boolean>();

  @HostBinding('class.chat-collapsed') get isCollapsed() { return this.collapsed; }

  messages: ChatMessage[] = [];
  newMessage = '';

  private apollo = inject(Apollo);
  private destroyRef = inject(DestroyRef);
  private sanitizer = inject(DomSanitizer);

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['gameId'] && this.gameId) {
      this.subscribeToChat();
      this.loadHistory();
    }
  }

  ngOnDestroy(): void {}

  private loadHistory(): void {
    this.apollo.watchQuery<{ chatMessages: ChatMessage[] }>({
      query: GET_CHAT_MESSAGES,
      variables: { gameId: this.gameId }
    }).valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: ({ data }) => {
        if (data?.chatMessages) {
          this.messages = data.chatMessages;
          setTimeout(() => this.scrollToBottom(), 100);
        }
      }
    });
  }

  private subscribeToChat(): void {
    this.apollo.subscribe<{ onChatMessage: ChatMessage }>({
      query: ON_CHAT_MESSAGE,
      variables: { gameId: this.gameId }
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: ({ data }) => {
        if (data?.onChatMessage) {
          this.messages = [...this.messages, data.onChatMessage].slice(-100);
          setTimeout(() => this.scrollToBottom(), 50);
        }
      },
      error: (err) => console.error('Chat subscription error:', err)
    });
  }

  sendMessage(): void {
    const content = this.newMessage.trim();
    if (!content || !this.gameId) return;

    this.apollo.mutate({
      mutation: SEND_CHAT_MESSAGE,
      variables: { input: { gameId: this.gameId, content } }
    }).subscribe({
      error: (err) => console.error('Chat send error:', err)
    });

    this.newMessage = '';
  }

  linkify(text: string): SafeHtml {
    const urlRegex = /(https?:\/\/[^\s<>"'{}|\\^`[\]]+)|(www\.[^\s<>"'{}|\\^`[\]]+)/gi;
    const escaped = text
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;');

    const linked = escaped.replace(urlRegex, (match) => {
      const href = match.startsWith('http') ? match : `https://${match}`;
      return `<a href="${href}" target="_blank" rel="noopener noreferrer" class="chat-link">${match}</a>`;
    });

    return this.sanitizer.bypassSecurityTrustHtml(linked);
  }

  private scrollToBottom(): void {
    const el = document.querySelector('.chat-messages');
    if (el) {
      el.scrollTop = el.scrollHeight;
    }
  }

  onKeyEnter(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }
}
