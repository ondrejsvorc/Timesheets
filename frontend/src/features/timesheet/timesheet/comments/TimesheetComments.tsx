import { useRef, useState } from "react";
import { useAsyncValue, useRevalidator } from "react-router";
import { SubPageHeader, SubPageTitle } from "@/components/shared/layout/SubPageHeader";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import { Texts } from "@/constants/texts";
import type { TimesheetComment } from "./Comment";
import { addTimesheetComment, type TimesheetCommentsScope } from "./commentsApi";
import { StatusChangeCommentEntry } from "./StatusChangeCommentEntry";

interface TimesheetCommentsProps {
  scope: TimesheetCommentsScope;
}

export const TimesheetComments = ({ scope }: TimesheetCommentsProps) => {
  const items = useAsyncValue() as TimesheetComment[];
  const revalidator = useRevalidator();
  const MAX_COMMENT_LENGTH = 500;
  const [draft, setDraft] = useState("");
  const [isSending, setIsSending] = useState(false);
  const [sendError, setSendError] = useState<string | null>(null);
  const textareaRef = useRef<HTMLTextAreaElement | null>(null);

  const roleLabel = (role: string) => {
    switch (role) {
      case "Employee":
        return Texts.roleEmployee;
      case "Manager":
        return Texts.roleManager;
      case "Controller":
        return Texts.roleController;
      default:
        return role;
    }
  };

  const onSend = async () => {
    if (isSending) return;
    setIsSending(true);
    setSendError(null);
    try {
      const controller = new AbortController();
      await addTimesheetComment(scope, { text: draft }, controller.signal);
      setDraft("");
      revalidator.revalidate();
      requestAnimationFrame(() => textareaRef.current?.focus());
    } catch (e) {
      setSendError(e instanceof Error ? e.message : Texts.sendCommentFailed);
    } finally {
      setIsSending(false);
    }
  };

  return (
    <>
      <SubPageHeader>
        <SubPageTitle>{Texts.comments}</SubPageTitle>
      </SubPageHeader>
      <div className="pb-8 space-y-4">
        <div className="rounded-md border bg-card p-4 md:p-5 space-y-5">
          {items.length === 0 ? (
            <div className="text-sm text-muted-foreground">{Texts.noCommentsYet}</div>
          ) : (
            items.map((c) =>
              c.type === "statusChange" ? (
                <StatusChangeCommentEntry key={c.id} createdAt={c.createdAt} statusChange={c.statusChange} />
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
            placeholder={Texts.writeCommentPlaceholder}
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
              {isSending ? Texts.sending : Texts.send}
            </Button>
          </div>
        </div>
      </div>
    </>
  );
};
