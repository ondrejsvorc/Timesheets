import { useRef, useState } from "react";
import { useRevalidator } from "react-router";
import { SubPageHeader, SubPageTitle } from "@/components/shared/layout/SubPageHeader";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import { Texts } from "@/constants/texts";
import { addTimesheetComment } from "../api/addTimesheetComment";
import type { TimesheetComment } from "./Comment";
import { StatusChangeCommentEntry } from "./StatusChangeCommentEntry";

export interface TimesheetCommentsScope {
  employeeId: string;
  year: number;
  month: number;
}

interface TimesheetCommentsProps {
  scope: TimesheetCommentsScope;
  comments: TimesheetComment[];
}

export const TimesheetComments = ({ scope, comments }: TimesheetCommentsProps) => {
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
    if (isSending) {
      return;
    }

    setIsSending(true);
    setSendError(null);
    try {
      const controller = new AbortController();
      await addTimesheetComment(
        {
          employeeId: scope.employeeId,
          year: scope.year,
          month: scope.month,
          text: draft,
        },
        controller.signal,
      );
      setDraft("");
      revalidator.revalidate();
      requestAnimationFrame(() => textareaRef.current?.focus());
    } catch (error) {
      setSendError(error instanceof Error ? error.message : Texts.sendCommentFailed);
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
          {comments.length === 0 ? (
            <div className="text-sm text-muted-foreground">{Texts.noCommentsYet}</div>
          ) : (
            comments.map((comment) =>
              comment.type === "statusChange" ? (
                <StatusChangeCommentEntry key={comment.id} createdAt={comment.createdAt} statusChange={comment.statusChange} />
              ) : (
                <div key={comment.id} className="rounded-md border bg-background px-3 py-3 md:px-4 md:py-3.5">
                  <div className="flex flex-wrap items-baseline justify-between gap-x-3 gap-y-1">
                    <div className="text-sm font-medium text-foreground">
                      {comment.author.name}
                      <span className="ml-2 text-xs font-normal text-muted-foreground">({roleLabel(comment.author.role)})</span>
                    </div>
                    <div className="text-xs text-muted-foreground tabular-nums">{new Date(comment.createdAt).toLocaleString("cs-CZ")}</div>
                  </div>
                  <div className="mt-2 text-sm leading-6 text-foreground/90 whitespace-pre-wrap">{comment.text}</div>
                </div>
              ),
            )
          )}
        </div>

        <div className="space-y-2">
          <Textarea
            ref={textareaRef}
            value={draft}
            onChange={(event) => setDraft(event.currentTarget.value)}
            placeholder={Texts.writeCommentPlaceholder}
            disabled={isSending}
            maxLength={MAX_COMMENT_LENGTH}
            className="w-full max-h-40 resize-none"
            onKeyDown={(event) => {
              if (event.key === "Enter" && !event.shiftKey) {
                event.preventDefault();
                if (draft.trim().length === 0) {
                  return;
                }
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
