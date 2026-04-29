// combination.ts

export const DSL_CONFIG = {
    SEPARATOR: '->',
    SIDES: ['left', 'right'],
    KINDS: {
        strike: ['jab', 'cross', 'hook', 'uppercut', 'elbow', 'knee'],
        kick: ['lowkick', 'roundkick', 'teep', 'sidekick'],
    }
} as const;

type StepKinds = typeof DSL_CONFIG.KINDS;
type StepKind<T extends keyof StepKinds> = StepKinds[T][number];

export type Step =
    | { type: 'unknown'; raw: string }
    | {
          [K in keyof StepKinds]: {
              type: K;
              kind: StepKind<K>;
              side: Side;
              raw: string;
          }
      }[keyof StepKinds];

export type ValidStep = Exclude<Step, { type: 'unknown' }>;

export type Side = typeof DSL_CONFIG.SIDES[number];
export type Strike = typeof DSL_CONFIG.KINDS.strike[number];
export type Kick = typeof DSL_CONFIG.KINDS.kick[number];

export type Combination = {
    id: string;
    name: string;
    dsl: string;
    steps: ValidStep[];
    createdAt: string;
    updatedAt: string;
};

// parser.ts

export const parseSteps = (dsl: string): ValidStep[] => {
    return dsl.split(DSL_CONFIG.SEPARATOR).map(parseStep).filter(isValidStep);
};

const parseStep = (text: string): Step => {
    const raw = text.trim().toLowerCase();
    if (!raw) return { type: 'unknown', raw: '' };

    const side = parseSide(raw);
    if (!side) return { type: 'unknown', raw: raw };

    const strike = parseStrike(raw);
    if (strike) return { type: 'strike', kind: strike, side, raw: text.trim() };

    const kick = parseKick(raw);
    if (kick) return { type: 'kick', kind: kick, side, raw: text.trim() };

    return { type: 'unknown', raw: raw };;
}

const parseSide = (text: string): Side | null => {
    return DSL_CONFIG.SIDES.find(side => text.includes(side)) ?? null;
};

const parseStrike = (text: string): Strike | null => {
    return DSL_CONFIG.KINDS.strike.find(strike => text.includes(strike)) ?? null;
};

const parseKick = (text: string): Kick | null => {
    return DSL_CONFIG.KINDS.kick.find(kick => text.includes(kick)) ?? null;
};

const isValidStep = (step: Step): step is ValidStep => {
    return step.type !== 'unknown';
};

// store.ts

export interface CombinationsState {
    items: Combination[];
    add: (name: string, dsl: string) => void;
    update: (id: string, name: string, dsl: string) => void;
    delete: (id: string) => void;
}