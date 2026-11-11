export interface DeckPreset {
  label: string;
  value: string;   // CSV deck setup string
  example: string;  // Short example for display
  /** Longer name shown as a tooltip, e.g. the usual name of a renamed deck. */
  hint?: string;
}

export const DECK_PRESETS: DeckPreset[] = [
  {
    label: 'Fibonacci',
    value: '0,0.5,1,2,3,5,8,13,❓,⏸️',
    example: '0, ½, 1, 2, 3, 5, 8, 13...'
  },
  {
    label: 'Fibonacci v2',
    hint: 'Modified Fibonacci: larger items round to 20, 40 and 100',
    value: '0,1,2,3,5,8,13,20,40,100,❓,⏸️',
    example: '0, 1, 2, 3, 5, 8, 13, 20, 40, 100...'
  },
  {
    label: 'T-shirt sizes',
    value: 'XS,S,M,L,XL,XXL,❓,⏸️',
    example: 'XS, S, M, L, XL, XXL...'
  },
  {
    label: 'Powers of 2',
    value: '1,2,4,8,16,32,64,❓,⏸️',
    example: '1, 2, 4, 8, 16, 32, 64...'
  }
];

export const DEFAULT_DECK = DECK_PRESETS[0].value;
