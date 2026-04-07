import { useEffect, useMemo, useRef, useState } from "react";
import { GenericSkeleton } from "@/components/shared/data/GenericSkeleton";
import { SubPageHeader, SubPageTitle } from "@/components/shared/layout/SubPageHeader";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import type { TimesheetComment } from "./Comment";
import { addTimesheetComment, listTimesheetComments } from "./commentsApi";

export const TimesheetComments = () => {
  const MAX_COMMENT_LENGTH = 500;
  const threadKey = useMemo(() => "timesheet:mock", []);
  const [items, setItems] = useState<TimesheetComment[] | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [draft, setDraft] = useState("");
  const [isSending, setIsSending] = useState(false);
  const [sendError, setSendError] = useState<string | null>(null);
  const textareaRef = useRef<HTMLTextAreaElement | null>(null);

  const roleLabel = (role: string) => {
    switch (role) {
      case "Employee":
        return "Zaměstnanec";
      case "Manager":
        return "Manažer";
      case "Controller":
        return "Kontrolor";
      default:
        return role;
    }
  };

  useEffect(() => {
    const controller = new AbortController();
    (async () => {
      try {
        setLoadError(null);
        const res = await listTimesheetComments(threadKey, controller.signal);
        setItems(res);
      } catch (e) {
        if (e instanceof DOMException && e.name === "AbortError") return;
        setLoadError(e instanceof Error ? e.message : "Nepodařilo se načíst komentáře.");
        setItems([]);
      }
    })();
    return () => controller.abort();
  }, [threadKey]);

  const onSend = async () => {
    if (isSending) return;
    setIsSending(true);
    setSendError(null);
    try {
      const controller = new AbortController();
      const created = await addTimesheetComment(threadKey, { text: draft }, controller.signal);
      setItems((prev) => [...(prev ?? []), created]);
      setDraft("");
      requestAnimationFrame(() => textareaRef.current?.focus());
    } catch (e) {
      setSendError(e instanceof Error ? e.message : "Nepodařilo se odeslat komentář.");
    } finally {
      setIsSending(false);
    }
  };

  return (
    <>
      <SubPageHeader>
        <SubPageTitle>Komentáře</SubPageTitle>
      </SubPageHeader>
      <div className="pb-8 space-y-4">
        {!items ? (
          <GenericSkeleton />
        ) : (
          <>
            {loadError && <div className="text-sm text-destructive">{loadError}</div>}

            <div className="rounded-md border bg-card p-4 md:p-5 space-y-5">
              {items.length === 0 ? (
                <div className="text-sm text-muted-foreground">Zatím žádné komentáře.</div>
              ) : (
                items.map((c) =>
                  c.type === "system" ? (
                    <div key={c.id} className="rounded-md border bg-muted/40 px-3 py-2 text-xs text-muted-foreground">
                      <span className="font-medium text-foreground/80">Systém</span> <span className="mx-1">·</span>
                      <span>{new Date(c.createdAt).toLocaleString("cs-CZ")}</span>
                      <span className="mx-1">·</span>
                      <span>{c.text}</span>
                    </div>
                  ) : (
                    <div key={c.id} className="rounded-md border bg-background px-3 py-3 md:px-4 md:py-3.5">
                      <div className="flex flex-wrap items-baseline justify-between gap-x-3 gap-y-1">
                        <div className="text-sm font-medium text-foreground">
                          {c.author.name}
                          <span className="ml-2 text-xs font-normal text-muted-foreground">({roleLabel(c.author.role)})</span>
                        </div>
                        <div className="text-xs text-muted-foreground tabular-nums">{new Date(c.createdAt).toLocaleString("cs-CZ")}</div>
                      </div>
                      <div className="mt-2 text-sm leading-6 text-foreground/90 whitespace-pre-wrap">{c.text}</div>
                    </div>
                  ),
                )
              )}
            </div>

            <div className="space-y-2">
              <Textarea
                ref={textareaRef}
                value={draft}
                onChange={(e) => setDraft(e.currentTarget.value)}
                placeholder="Napište komentář…"
                disabled={isSending}
                maxLength={MAX_COMMENT_LENGTH}
                className="w-full max-h-40 resize-none"
                onKeyDown={(e) => {
                  if (e.key === "Enter" && !e.shiftKey) {
                    e.preventDefault();
                    if (draft.trim().length === 0) return;
                    void onSend();
                  }
                }}
              />
              <div className="text-xs text-muted-foreground text-right tabular-nums">
                {draft.length}/{MAX_COMMENT_LENGTH}
              </div>
              {sendError && <div className="text-sm text-destructive">{sendError}</div>}
              <div className="flex justify-end">
                <Button type="button" onClick={onSend} disabled={isSending || draft.trim().length === 0}>
                  {isSending ? "Odesílám…" : "Odeslat"}
                </Button>
              </div>
            </div>
          </>
        )}
      </div>
    </>
  );
};

// Intentionally minimal for now; keep all comments UI in `TimesheetComments`.
