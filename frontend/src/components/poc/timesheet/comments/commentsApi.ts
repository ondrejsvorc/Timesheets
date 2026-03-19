import type { TimesheetComment, TimesheetCommentAuthor } from "./Comment";

type ThreadKey = string;

const AUTHOR_JAN: TimesheetCommentAuthor = { name: "Jan Novák", role: "Employee" };

const store = new Map<ThreadKey, TimesheetComment[]>();

function delay(ms: number, signal?: AbortSignal): Promise<void> {
  return new Promise((resolve, reject) => {
    if (signal?.aborted) {
      reject(new DOMException("Aborted", "AbortError"));
      return;
    }
    const id = setTimeout(resolve, ms);
    signal?.addEventListener(
      "abort",
      () => {
        clearTimeout(id);
        reject(new DOMException("Aborted", "AbortError"));
      },
      { once: true },
    );
  });
}

function nowIso(): string {
  return new Date().toISOString();
}

function newId(): string {
  // Good enough for mock; backend will replace.
  return `c_${Date.now()}_${Math.random().toString(16).slice(2)}`;
}

function seedIfMissing(threadKey: ThreadKey) {
  if (store.has(threadKey)) return;
  store.set(threadKey, [
    {
      id: newId(),
      type: "message",
      createdAt: nowIso(),
      author: AUTHOR_JAN,
      text: "Ahoj, posílám výkaz k rychlé kontrole.",
    },
  ]);
}

export async function listTimesheetComments(threadKey: ThreadKey, signal?: AbortSignal): Promise<TimesheetComment[]> {
  seedIfMissing(threadKey);
  await delay(350, signal);
  return (store.get(threadKey) ?? []).slice();
}

export async function addTimesheetComment(
  threadKey: ThreadKey,
  input: { text: string },
  signal?: AbortSignal,
): Promise<TimesheetComment> {
  seedIfMissing(threadKey);
  await delay(200, signal);

  const text = input.text.trim();
  if (!text) {
    throw new Error("Komentář nesmí být prázdný.");
  }

  const comment: TimesheetComment = {
    id: newId(),
    type: "message",
    createdAt: nowIso(),
    author: AUTHOR_JAN,
    text,
  };

  store.set(threadKey, [...(store.get(threadKey) ?? []), comment]);
  return comment;
}

