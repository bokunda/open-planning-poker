import { ChangeDetectionStrategy, Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { ApiCollectionOfGamePlayer, Settings, SettingsDetailsResult, Ticket } from '../../../../graphql/graphql-gateway.service';

@Component({
  selector: 'app-voting',
  templateUrl: './voting.component.html',
  styleUrl: './voting.component.scss',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class VotingComponent implements OnInit {

  @Input() gameSettings: SettingsDetailsResult | undefined;
  @Input() players: ApiCollectionOfGamePlayer | undefined;
  @Input() ticket: Ticket | undefined;

  @Output() onCreateNewTicket = new EventEmitter<void>();
  @Output() onVoteAgain = new EventEmitter<void>();
  @Output() onRevealCards = new EventEmitter<void>();

  voteOptions: string[] = [];
  selectedOption: string | null = null;

  ngOnInit(): void {
    const settings = this.gameSettings as Settings;

    if (!settings) { return; }

    this.voteOptions = settings.deckSetup.split(',');
  }

  selectOption(option: string): void {
    this.selectedOption = option;
  }

}
