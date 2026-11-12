export interface DeckPreset {
  label: string;
  /** Translation key of the label (and `<key>Hint` for the hint). */
  key: string;
  value: string;   // CSV deck setup string
  example: string;  // Short example for display
  /** Longer name shown as a tooltip, e.g. the usual name of a renamed deck. */
  hint?: string;
}

export const DECK_PRESETS: DeckPreset[] = [
  {
    label: 'Fibonacci',
    key: 'deck.fibonacci',
    value: '0,0.5,1,2,3,5,8,13,❓,⏸️',
    example: '0, ½, 1, 2, 3, 5, 8, 13...'
  },
  {
    label: 'Fibonacci v2',
    key: 'deck.fibonacci2',
    hint: 'Modified Fibonacci: larger items round to 20, 40 and 100',
    value: '0,1,2,3,5,8,13,20,40,100,❓,⏸️',
    example: '0, 1, 2, 3, 5, 8, 13, 20, 40, 100...'
  },
  {
    label: 'T-shirt sizes',
    key: 'deck.tshirt',
    value: 'XS,S,M,L,XL,XXL,❓,⏸️',
    example: 'XS, S, M, L, XL, XXL...'
  },
  {
    label: 'Powers of 2',
    key: 'deck.powers',
    value: '1,2,4,8,16,32,64,❓,⏸️',
    example: '1, 2, 4, 8, 16, 32, 64...'
  }
];

export const DEFAULT_DECK = DECK_PRESETS[0].value;
