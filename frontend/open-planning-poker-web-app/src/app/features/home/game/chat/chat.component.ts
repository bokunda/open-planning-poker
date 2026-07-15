import { Component, Input, OnDestroy, OnInit, inject, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Apollo } from 'apollo-angular';
import { ChatMessage } from '../../../../graphql/graphql-gateway.service';
import { ON_CHAT_MESSAGE } from '../gql/onChatMessage.graphql';
import { SEND_CHAT_MESSAGE } from '../gql/sendChatMessage.graphql';

@Component({
  selector: 'app-chat',
  templateUrl: './chat.component.html',
  styleUrl: './chat.component.scss',
  standalone: false
})
export class ChatComponent implements OnInit {
  @Input() gameId: string = '';
  @Input() currentUserId: string = '';

  messages: ChatMessage[] = [];
  newMessage = '';
  expanded = false;

  private apollo = inject(Apollo);
  private destroyRef = inject(DestroyRef);

  ngOnInit(): void {
    if (this.gameId) {
      this.apollo.subscribe<{ onChatMessage: ChatMessage }>({
        query: ON_CHAT_MESSAGE,
        variables: { gameId: this.gameId }
      }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: ({ data }) => {
          if (data?.onChatMessage) {
            this.messages = [...this.messages, data.onChatMessage].slice(-100);
            setTimeout(() => this.scrollToBottom(), 50);
          }
        }
      });
    }
  }

  ngOnDestroy(): void {}

  sendMessage(): void {
    const content = this.newMessage.trim();
    if (!content || !this.gameId) return;

    this.apollo.mutate({
      mutation: SEND_CHAT_MESSAGE,
      variables: { input: { gameId: this.gameId, content } }
    }).subscribe();

    this.newMessage = '';
  }

  toggleChat(): void {
    this.expanded = !this.expanded;
    if (this.expanded) {
      setTimeout(() => this.scrollToBottom(), 100);
    }
  }

  get unreadCount(): number {
    // Simple: count messages received while collapsed
    return this.expanded ? 0 : this.messages.length;
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
