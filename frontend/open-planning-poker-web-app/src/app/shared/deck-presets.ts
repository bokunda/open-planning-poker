export interface DeckPreset {
  label: string;
  value: string;   // CSV deck setup string
}

export const DECK_PRESETS: DeckPreset[] = [
  {
    label: 'Fibonacci',
    value: '0,0.5,1,2,3,5,8,13,❓,⏸️'
  },
  {
    label: 'Modified Fibonacci',
    value: '0,1,2,3,5,8,13,20,40,100,❓,⏸️'
  },
  {
    label: 'T-Shirt Sizes',
    value: 'XS,S,M,L,XL,XXL,❓,⏸️'
  },
  {
    label: 'Powers of 2',
    value: '1,2,4,8,16,32,64,❓,⏸️'
  }
];

export const DEFAULT_DECK = DECK_PRESETS[0].value;
